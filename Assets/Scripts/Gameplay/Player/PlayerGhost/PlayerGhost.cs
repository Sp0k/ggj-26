using Unity.Cinemachine;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using static FirstPersonController;

namespace Unity.FPSSample_2
{
    public partial class PlayerGhost : GhostMonoBehaviour, IUpdateClient, IUpdateServer
    {
        [field: SerializeField] public AssetReferenceGameObject ProjectilePrefabAR { get; private set; }
        [SerializeField] private Vector3 m_CameraRotation;
        [SerializeField] private GameObject m_OwnerVisuals;
        [SerializeField] private GameObject m_OtherPlayerVisuals;
        [SerializeField] private SoundDef m_SpawnSFX;
        [field: SerializeField] public Transform CameraTarget { get; private set; }
        [field: SerializeField] public Transform ReticlePoint { get; private set; }
        [field: SerializeField] public Transform ShotOrigin { get; private set; }
        [field: FormerlySerializedAs("<VisualShotOrigin>k__BackingField")] [field: SerializeField] 
        public Transform VisualShotOrigin1P { get; private set; }
        [field: SerializeField] public Transform VisualShotOrigin3P { get; private set; }
        [field: SerializeField] public Transform HoldPoint { get; private set; }
        private Rigidbody _heldObjectBody;
        private Transform _heldObjectOriginalParent;
        private bool _heldObjectKinematic;
        private CollisionDetectionMode _heldObjectCdm;

        [Header("Manual Aiming Setup")]
        [SerializeField] private Animator m_Animator3P;
        private static readonly int AimPitchHash = Animator.StringToHash("AimPitch");
        
        public int PlayerIndex { get; private set; }
        public int InputUserId { get; set; } = -1;
        public PlayerInput ServerMovementInput { get; set; }
        public ControllerConsts ControllerConsts { get; private set; }

        #region Cached Player Components
        private FirstPersonController m_Controller;
        public FirstPersonController Controller => m_Controller;

        #endregion

        [field: SerializeField] public GameObject MainCameraPrefab { get; private set; }

        private Camera m_PlayerCamera;
        private Animator _animatorCharacter;
        private Vector3 m_ReticleVector;

        private CinemachineTargetGroup m_TargetGroup;
        private CinemachinePositionComposer m_PositionComposer;
        private CinemachineCamera m_CinemachineCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_NextPredictionId = 1;
        }
        
        private static uint s_NextPredictionId = 1;

        public static uint GetNextPredictionID()
        {
            return s_NextPredictionId++;
        }

        public Camera GetPlayerCamera()
        {
            return m_PlayerCamera;
        }

        public CinemachineCamera GetPlayerCinemachineCamera()
        {
            return m_CinemachineCamera;
        }

        public CinemachinePositionComposer GetPositionComposer()
        {
            return m_PositionComposer;
        }

        public struct PlayerData : IComponentData
        {
            [GhostField] public FixedString128Bytes Name;
            public Entity ViewEntity;
            public Entity ControlledEntity;
        }

        public void Awake()
        {
            GetRequiredComponent(out m_Controller);
            m_ReticleVector = ReticlePoint.localPosition;
        }

        public void HoldTick(bool holdPressed, Vector3 rayOrigin, Vector3 rayDir, float grabRange, int layerMask)
        {
            if (!holdPressed)
            {
                if (_heldObjectBody != null) ReleaseHeldObject();
                return;
            }

            if (_heldObjectBody == null)
            {
                TryGrab(rayOrigin, rayDir, grabRange, layerMask);
            }
        }

         void TryGrab(Vector3 rayOrigin, Vector3 rayDir, float grabRange, int layerMask)
        {
            if (HoldPoint == null) return;

            if (UnityEngine.Physics.Raycast(rayOrigin, rayDir, out UnityEngine.RaycastHit hit, grabRange, layerMask))
            {
                var rb = hit.rigidbody;
                if (rb == null) return;

                _heldObjectBody = rb;
                _heldObjectOriginalParent = rb.transform.parent;
                _heldObjectKinematic = rb.isKinematic;
                _heldObjectCdm = rb.collisionDetectionMode;

                if (!rb.CompareTag("Pickable")) return;

                rb.isKinematic = true;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                rb.transform.SetParent(HoldPoint, worldPositionStays: true);
                rb.transform.localPosition = Vector3.zero;
                rb.transform.localRotation = Quaternion.identity;
            }
        }

        private void UpdateHeldTransform()
        {
            if (HoldPoint == null || _heldObjectBody == null) return;

            _heldObjectBody.transform.position = HoldPoint.position;
            _heldObjectBody.transform.rotation = HoldPoint.rotation;
        }

        private void ReleaseHeldObject()
        {
            if (_heldObjectBody == null) return;

            _heldObjectBody.transform.SetParent(_heldObjectOriginalParent, worldPositionStays: true);
            _heldObjectBody.isKinematic = _heldObjectKinematic;
            _heldObjectBody.collisionDetectionMode = _heldObjectCdm;

            _heldObjectBody = null;
            _heldObjectOriginalParent = null;
        }
        
        private void LateUpdate()
        {
            // This logic is for visual clients only
            if ((Role != MultiplayerRole.ClientProxy && Role != MultiplayerRole.ClientOwned) || m_Animator3P == null)
            {
                return;
            }

            var predictedPlayerGhost = ReadGhostComponentData<PredictedPlayerGhost>();
            var controllerState = predictedPlayerGhost.ControllerState;

            // matches vertical angle value in ClientInputReaderSystem
            float normalizedPitch = controllerState.PitchDegrees / 85f;

            m_Animator3P.SetFloat(AimPitchHash, normalizedPitch);
        }

        public override void OnGhostLinked()
        {
            bool isClientOwned = (Role == MultiplayerRole.ClientOwned);
            m_OwnerVisuals.SetActive(isClientOwned);
            m_OtherPlayerVisuals.SetActive(!isClientOwned);

            if (Role != MultiplayerRole.Server)
            {
                _animatorCharacter = GetComponent<Animator>();
                // spawn SFX
                if (m_SpawnSFX != null)
                {
                    GameManager.Instance.SoundSystem.CreateEmitter(m_SpawnSFX, transform.position);
                }
            }

            if (isClientOwned)
            {
                var predictedPlayer = ReadGhostComponentData<PredictedPlayerGhost>();
                PlayerIndex = predictedPlayer.InputIndex;
                // create camera
                CreateClientCamera();

                // Add AudioListener to client position and ensure all other AudioListeners are disabled
                var audioListeners = Resources.FindObjectsOfTypeAll<AudioListener>();
                foreach (var a in audioListeners)
                {
                    a.enabled = false;
                }
                m_OwnerVisuals.AddComponent<AudioListener>();
                
                // Attach the listener to the player model rather than the camera
                GameManager.Instance.SoundSystem.SetListenerTransform(m_OwnerVisuals.transform);    
            }
            else if (Role == MultiplayerRole.ClientProxy)
            {
                // no physics required
                Controller.CharacterController.enabled = false;
            }

            gameObject.layer = (Role == MultiplayerRole.Server)
                ? (int)LayerIndex.ServerPlayer
                : (int)LayerIndex.ClientPlayer;

            PlayerGhostManager.TryGetInstanceByRole(Role, out var playerManager);
            playerManager.Register(this);
        }

        public override void OnGhostPreDestroy()
        {
            if (PlayerGhostManager.TryGetInstanceByRole(Role, out var playerManager))
            {
                playerManager.Unregister(this);
            }
        }

        void AttachPlayerViewCamera()
        {
            var playerCamera = GameObject.FindFirstObjectByType<Camera>();
            if (playerCamera != null)
            {
                playerCamera.transform.parent = transform.Find("ViewPoint");
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;
            }
        }

        private void CreateClientCamera()
        {
            //Disable current camera
            var existingCamera = FindFirstObjectByType<Camera>();
            if (existingCamera != null)
            {
                existingCamera.enabled = false;
            }

            // spawn the camera
            var mainCameraInstance = Instantiate(MainCameraPrefab, CameraTarget.transform);
            mainCameraInstance.transform.localPosition = Vector3.zero;
            mainCameraInstance.name = $"MainCamera_{PlayerIndex}";

            m_PlayerCamera = mainCameraInstance.GetComponent<Camera>();

            var audioListener = mainCameraInstance.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                GameManager.Instance.SoundSystem.SetListenerTransform(audioListener.transform);
            }

            Utils.SetCursorVisible(false);
        }

        public void UpdateServer(float deltaTime)
        {
            // Server does not need to do anything for now regarding PlayerGhost
        }

        public void UpdateClient(float deltaTime)
        {
            var predictedPlayerGhost = ReadGhostComponentData<PredictedPlayerGhost>();
            var controllerState = predictedPlayerGhost.ControllerState;
            if (Role == MultiplayerRole.ClientOwned)
            {
                CameraTarget.transform.rotation = Quaternion.Euler(controllerState.PitchDegrees,
                    Camera.main.transform.rotation.eulerAngles.y,
                    Camera.main.transform.rotation.eulerAngles.z);
            }

            var rot = Quaternion.Euler(controllerState.PitchDegrees, 0.0f, 0.0f);
            ReticlePoint.localPosition = rot * m_ReticleVector;

            //TODO: The following is a temporary fix for animation root moves (Robot Jump for example)
            m_OtherPlayerVisuals.transform.localPosition = Vector3.zero;
            m_OtherPlayerVisuals.transform.localRotation = Quaternion.identity;
        }

        public bool SetPlayerPositionFromRPC(float3 rpcPosition, float positionErrorSq)
        {
            var predictedPlayerGhost = ReadGhostComponentData<PredictedPlayerGhost>();
            var controllerState = predictedPlayerGhost.ControllerState;

            float positionError = math.distancesq(controllerState.CurrentPosition, rpcPosition);

            //allow the current player position to be altered by the client but only within a certain tolerance
            //(this is to avoid sliding during some position locked animations caused by the player predicting ahead of the server)
            if (positionError <= (positionErrorSq))
            {
                controllerState.CurrentPosition = rpcPosition;
                predictedPlayerGhost.ControllerState = controllerState;
                WriteGhostComponentData(predictedPlayerGhost);

                return true;
            }

            return false;
        }
    }
}