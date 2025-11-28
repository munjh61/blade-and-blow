using UnityEngine;
using System;
using Game.Domain;
using System.Collections;




#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonControllerReborn : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;


        [Header("Roll Settings")]
        public float RollSpeed = 3f;
        public float RollDuration = 0.8f;

        private bool _isRolling = false;
        private Vector3 _rollDirection;
        private float _rollTimer = 0f;

        [Header("Jump Settings")]
        public int MaxJumpCount = 1; // 최대 점프 횟수 (1단 점프 + 1단 공중 점프)
        private int _jumpCount = 0;   // 현재 점프 횟수

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        //weapon prefab
        //private string[] weaponPrefabName = { "drop_sword", "drop_bow", "drop_wand" };

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDRoll;
        // 근접공격
        private int _animIDSwing;
        // 활 공격
        private int _animIDAim;
        private int _animIDFire;
        private int _animIDPullString;
        // 마법 공격
        private int _animIDMagicAim;
        private int _animIDMagicFire;
        private int _animIDMagicCharge;

        // 뒤짐
        private int _animIDDeath;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;
        [SerializeField] private bool _isOwner;

        [Header("Weapon Settings")]
        public GameObject nearObject;
        private CharacterEquipmentReborn _ce;
        private PlayerManagerPunBehaviour pm;

        // 공격 관련 변수
        float fireDelay;
        bool isFireReady;

        // 공격입력 시점
        private float pressStartTime;
        bool isAiming = false; // 조준 상태

        [SerializeField] private Transform _aimOrigin;
        public void SetAimOrigin(Transform t) => _aimOrigin = t;
        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }

        // 스테미너 관리
        [Header("Stamina / Action Costs")]
        public int RollStaminaCost = 45;        // 구르기 코스트
        public int SprintMinStamina = 10;       // 달리기 시작 최소치
        private bool _prevSprint;               // 달리기 엣지 검출용
        private bool _authSprintActive = false; // 달리기 권위 적용

        private void Awake()
        {
            _ce = GetComponent<CharacterEquipmentReborn>();
            
            _controller = GetComponent<CharacterController>();
            _hasAnimator = TryGetComponent(out _animator);
            _input = GetComponent<StarterAssetsInputs>();
            pm = FindFirstObjectByType<PlayerManagerPunBehaviour>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput) _playerInput.enabled = false;
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
        }

        private void Start()
        {
            AssignAnimationIDs();

            // 사용자 설정 값 불러오는 이벤트 등록
            RegisterSettingsEvents();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            if (pm != null)
                pm.StaminaApplied += OnStaminaApplied_LocalOnly;
        }

        public void SetAuthority(bool isOwner)
        {
            if (_isOwner == isOwner) return;
            _isOwner = isOwner;

#if ENABLE_INPUT_SYSTEM
            if (_playerInput != null) _playerInput.enabled = _isOwner;
#endif
            if (_input != null) _input.enabled = _isOwner;
        }

        private void FixedUpdate()
        {
            if (!_isOwner) return;

            if (_isDead)
            {
                HandleDeath();
                return; // 죽으면 다른 이동/점프 처리 중지
            }


            _hasAnimator = TryGetComponent(out _animator);

            GroundedCheck();
            JumpAndGravity();

            pm?.EnsureLocalDefaults();
            if (_input.sprint != _prevSprint)
            {
                // press
                if (_input.sprint)
                {
                    // 로컬 캐시로 1차 게이트 (없으면 낭비 전송 방지)

                    // Debug.Log($"[tpc] check spring PM is enabled={pm.isActiveAndEnabled}");
                    if (pm != null && pm.TryGetLocalState(out var d) && d.stamina >= SprintMinStamina)
                    {
                        pm.Local_SendSprint(true);
                        _authSprintActive = true;
                    }
                    else
                    {
                        if (pm != null && pm.TryGetLocalState(out var e))
                            // Debug.Log($"[tpc] actor={e.actor} stamina={e.stamina}");
                        _input.sprint = false;
                        _authSprintActive = false;
                    }
                }
                // release
                else
                {
                    pm?.Local_SendSprint(false);
                    _authSprintActive = false;
                }

                _prevSprint = _input.sprint;
            }

            // 롤 중이면 롤만 처리하고 리턴
            if (_isRolling)
            {
                HandleRoll();
                return;
            }

            if (_input.rollPressed && !_isRolling && Grounded)
            {
                bool canRoll = pm != null && pm.TryGetLocalState(out var d) && d.stamina >= RollStaminaCost;
                if (canRoll)
                {
                    // 의도 전송: TryAction(cost)
                    pm.Local_TrySpendStamina(RollStaminaCost);

                    // 로컬 예측: 즉시 롤 시작
                    StartRoll();
                }
            }
            _input.rollPressed = false; // 한 번 발동 후 초기화
            Move();
            Interaction();
            Drop();
            Attack();
        }

        private void OnStaminaApplied_LocalOnly(PlayerId actor, int stamina)
        {
            if (pm == null || !_isOwner) return;
            if (stamina <= 0) _authSprintActive = false;
        }

        private void OnDestroy()
        {
            UnregisterSettingsEvents();

            if (pm != null) pm.StaminaApplied -= OnStaminaApplied_LocalOnly;
        }

        private void LateUpdate()
        {
            if (!_isOwner) return;
            CameraRotation();
            if (CheckArrowAnim())
            {
                // Spine 본 가져오기
                Transform spine = _animator.GetBoneTransform(HumanBodyBones.Spine);
                if (spine == null) return;

                // 카메라 Pitch(위/아래 각도) 가져오기
                float cameraPitch = CinemachineCameraTarget.transform.eulerAngles.x;
                if (cameraPitch > 180f) cameraPitch -= 360f; // -180 ~ 180 범위로 변환

                // 기본 Spine 회전값
                Quaternion baseRotation = Quaternion.Euler(-90f, 0f, 0f);

                // 카메라 Pitch를 Y축에 적용
                Quaternion pitchRotation = Quaternion.Euler(0f, cameraPitch, 0f);

                // 최종 Spine 회전
                spine.localRotation = baseRotation * pitchRotation;
            }

        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            // Roll 추가
            _animIDRoll = Animator.StringToHash("Roll");
            // MeleeAttack 추가
            _animIDSwing = Animator.StringToHash("Swing");
            // Bow Attack 추가
            _animIDAim = Animator.StringToHash("Aim");
            _animIDFire = Animator.StringToHash("Fire");
            _animIDPullString = Animator.StringToHash("PullString");
            // Magic Attack 추가
            _animIDMagicAim = Animator.StringToHash("MagicAim");
            _animIDMagicFire = Animator.StringToHash("MagicFire");
            _animIDMagicCharge = Animator.StringToHash("MagicCharge");
            // Death 추가
            _animIDDeath = Animator.StringToHash("Death");
        }


        private bool _isDead = false;       // 사망 상태 체크
        private float _deathRiseSpeed = 0.8f; // 사망 시 위로 뜨는 초기 속도
        private float deathSequenceDuration = 5f; // 사망 후 씬 전환까지 시간
        public void OnDeath()
        {
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDDeath, true);
            }
            _isDead = true;
            _verticalVelocity = _deathRiseSpeed;

            // --- 관전 모드 시작 및 아이템 드랍 등 소유자만 처리해야 하는 로직 ---
            if (_isOwner) 
            {
                // 아이템 드랍은 소유자만 요청해야 합니다.
                PerformDrop();

                // 씬에 있는 SpectatorController의 싱글톤 인스턴스를 찾습니다.
                if (SpectatorController.Instance != null)
                {
                    // 자신의 CameraBinder 컴포넌트를 가져와 제어권을 넘겨줍니다.
                    CameraBinder myBinder = GetComponent<CameraBinder>();
                    SpectatorController.Instance.BeginSpectating(myBinder);
                }
            }
            // ---------------------------------
            StartCoroutine(DeactivateAfterDelay(deathSequenceDuration));
        }
        private IEnumerator DeactivateAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }

        private void HandleDeath()
        {
            if (!_isDead) return;

            // Y축으로 천천히 올라가기
            Vector3 rise = new Vector3(0, _deathRiseSpeed * Time.deltaTime, 0);
            _controller.Move(rise);
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);

            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                  QueryTriggerInteraction.Ignore);

            if (Grounded)
            {
                _jumpCount = 0; // 땅에 닿으면 점프 횟수 초기화
                //_verticalVelocity = -2f; // 땅에 있으면 속도 초기화
            }

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        [Header("Camera Settings")]
        [Tooltip("Mouse/Gamepad look sensitivity multiplier")]
        public float LookSensitivity = 3.0f;

        [Header("Sensitivity Mapping")]
        private Vector2 _userRange = new Vector2(1f, 10f);
        public float MinSensitivity = 0.5f;
        public float MaxSensitivity = 6.0f;

        private void RegisterSettingsEvents()
        {
            if (SettingsStore.Instance == null) return;

            // 초기값 반영
            LookSensitivity = SettingsStore.Instance.Current.mouseSensitivity;

            // 이벤트 구독
            SettingsStore.Instance.OnApplied += OnSettingsApplied;
        }

        private void UnregisterSettingsEvents()
        {
            if (SettingsStore.Instance == null) return;

            SettingsStore.Instance.OnApplied -= OnSettingsApplied;
        }

        private void OnSettingsApplied(UserSettings s)
        {
            LookSensitivity = MapSensitivity(s.mouseSensitivity);
        }

        private float MapSensitivity(float userVal)
        {
            float clamped = Mathf.Clamp(userVal, _userRange.x, _userRange.y);
            float t = Mathf.InverseLerp(_userRange.x, _userRange.y, clamped);
            return Mathf.Lerp(MinSensitivity, MaxSensitivity, t);
        }

        private void CameraRotation()
        {
            if (_input == null) return;
            if (LockCameraPosition) return;

            // look 입력이 있을 때만 처리
            if (_input.look.sqrMagnitude >= _threshold)
            {
                // Don't multiply mouse input by Time.deltaTime (same as 기존 템플릿)
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                float yawDelta = _input.look.x * deltaTimeMultiplier * LookSensitivity;
                float pitchDelta = _input.look.y * deltaTimeMultiplier * LookSensitivity;

                // 좌우(Yaw) -> 플레이어 자체 회전 (월드 Y축)
                // transform.Rotate 사용하면 부드럽게 바로 돌림
                transform.Rotate(0f, yawDelta, 0f, Space.Self);

                // 상하(Pitch) -> 카메라 타겟의 pitch 누적
                _cinemachineTargetPitch += pitchDelta;
            }

            // pitch clamp (BottomClamp, TopClamp는 필드로 이미 있음)
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // 플레이어 내부에 Main Camera가 있는 경우 회전 적용 (local X축만)
            //_mainCamera.transform.localRotation =
            //    Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, 0f, 0f);

            // 가상 카메라의 Follow Target을 회전시킴
            if (CinemachineCameraTarget != null)
            {
                CinemachineCameraTarget.transform.localRotation =
                    Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, 0f, 0f);
            }
        }


        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            bool wantSprint = _input.sprint && _authSprintActive;
            float targetSpeed = wantSprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // ------------------- 수정된 부분 ------------------

            // transform(플레이어) 기준으로 입력을 월드 벡터로 변환
            Vector3 localInput = new Vector3(_input.move.x, 0f, _input.move.y);
            Vector3 worldMove = transform.TransformDirection(localInput);

            // normalize (대각선 속도 보정이 필요하면 여기를 조정)
            if (worldMove.sqrMagnitude > 1f) worldMove.Normalize();

            // 이동 적용 (vertical velocity 포함)
            _controller.Move(worldMove * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);


            // --------------------------------------------------


            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    _jumpCount++; // 첫 점프
                    _input.jump = false;
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }
                // 공중에서 두 번째 점프`
                if (_input.jump && (_jumpCount < MaxJumpCount))
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    _jumpCount++; // 두 번째 점프
                    
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }
        // -------------------- 여기서부터 롤 기능 --------------------
        private void StartRoll()
        {
            _isRolling = true;
            _rollTimer = RollDuration;

            // 구르기 시작 시점 방향 고정
            _rollDirection = transform.forward;

            if (_hasAnimator)
                _animator.SetTrigger(_animIDRoll);
        }

        private void HandleRoll()
        {
            _controller.Move(_rollDirection * RollSpeed * Time.deltaTime);

            _rollTimer -= Time.deltaTime;
            if (_rollTimer <= 0f)
                _isRolling = false;
        }
        // ------------------------------------------------------------
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = UnityEngine.Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            if(!_isOwner ||_isDead) return;

            if(other.gameObject.layer == LayerMask.NameToLayer("InstantDeath")) 
            {
                if (pm != null)
                {
                    pm.RequestInstantKill("Sky Dive");
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.tag == "Weapon")
            {
                nearObject = other.gameObject;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.tag == "Weapon")
            {
                nearObject = null;
            }
        }

        void Interaction()
        {
            if (!_isOwner) return;
            if (_input.interaction && nearObject != null)
            {
                _input.interaction = false;
                if (nearObject.TryGetComponent<DropHandle>(out var dh))
                {
                    // nearObject 에서 DropHandle의 값으로 key 가져옴
                    // 이름 → 인덱스
                    int idx = _ce.FindWeaponIndexByName(dh.WeaponName);
                    if (idx >= 0)
                    {
                        // 줍기 시도 전달
                        PickupSignals.Request(dh.Token, idx);

                        // (선택) 로컬 예측 적용
                        _ce.ApplyEquipImmediate(idx);

                        // 장비 적용 전달
                        SelectedLoadout.SetEquip(idx);

                        nearObject = null;
                    }
                    else
                    {
                        Debug.LogWarning($"[TPC] Unknown weapon name: {dh.WeaponName}");
                    }
                }
            }
        }

        void Drop()
        {
            if (_input.drop)
            {
                _input.drop = false;
                PerformDrop();
            }
        }
        void PerformDrop() {
            int idx = _ce.GetEquippedId();
            if (idx < 0) return;

            string key = (_ce.weapons[idx] ? _ce.weapons[idx].name : null) ?? _ce.equippedWeapon;
            Vector3 dropPos = transform.position + transform.forward * 1.0f + Vector3.up * 0.5f;

            // 버리기 시도 전달
            DropSignals.Request(key, dropPos, Quaternion.identity);

            _ce.ApplyEquipImmediate(-1);
            SelectedLoadout.SetEquip(-1);
        }

        private bool CheckArrowAnim()
        {
            // 현재 레이어에서 재생 중인 애니메이션 클립 이름 확인
            AnimatorClipInfo[] clipInfos = _animator.GetCurrentAnimatorClipInfo(1);

            if (clipInfos.Length > 0)
            {
                string clipName = clipInfos[0].clip.name;
                if (clipName.StartsWith("Arrow"))
                {
                    return true;
                }
            }
            return false;
        }

        public void Attack()
        {
            var active = _ce.activatedWeapon;
            if (active == null) return;

            fireDelay += Time.deltaTime;
            isFireReady = active.rate < fireDelay;

            switch (active.type)
            {
                case Weapon.Type.Bow:
                    AttackBow(active);
                    break;

                case Weapon.Type.Sword:
                    AttackSword(active);
                    break;

                case Weapon.Type.Wand:
                    AttackWand(active);
                    break;
                // 새 무기 타입 추가 시 여기에 case 추가
                default:
                    break;
            }
        }


        void AttackSword(Weapon active)
        {
            if (_input.attackPressed && isFireReady && !_isRolling)
            {
                active.Use(new WeaponContext());
                _animator.SetTrigger(_animIDSwing);
                fireDelay = 0;
                _input.attackPressed = false;
            }
        }


        void AttackBow(Weapon active)
        {
            if (_input.attackPressed)
            {
                StartAiming();
                _input.attackPressed = false;
            }

            if (isAiming)
            {
                bool pulling = _input.attackHeld;
                if (_animator) _animator.SetBool(_animIDPullString, pulling);
            }

            if (_input.attackReleased)
            {
                float cd = Time.time - pressStartTime;

                //놓는 순간 shoot bow broadcast
                if (isAiming && isFireReady && cd >= active._bowMinChargeTime)
                {
                    if (_animator) _animator.SetTrigger(_animIDFire);
                    WeaponContext context = new WeaponContext
                    {
                        cameraTransform = _aimOrigin,
                        chargeDuration = cd,
                        shooterId = -1
                    };
                    
                    active.Use(context);   // 실제 발사
                    pm.Local_RequestShoot(_aimOrigin, cd);
                    fireDelay = 0f;
                }

                EndAiming();               // 조준 종료(항상)
                _input.attackReleased = false;
            }
        }

        public void aimProjectile()
        {
            // 에이밍 동기화 확인 해야됨
            Transform spine = _animator.GetBoneTransform(HumanBodyBones.Spine);
            if(_ce.activatedWeapon.type == Weapon.Type.Bow)
            {
                _ce.weapons[3].SetActive(true);
                spine.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            }
            else if (_ce.activatedWeapon.type == Weapon.Type.Wand)
            {
                _ce.weapons[4].SetActive(true);
            }
        }
        public void shootProjectile(Transform aimOrigin, float changeDuration, int shooterId)
        {
            WeaponContext context = new WeaponContext
            {
                cameraTransform = aimOrigin,
                chargeDuration = changeDuration,
                shooterId = shooterId,
            };

            _ce.activatedWeapon.Use(context);   // 실제 발사
            _ce.weapons[3].SetActive(false);    
            _ce.weapons[4].SetActive(false);
        }

        void StartAiming()
        {
            pressStartTime = Time.time;
            isAiming = true;
            if (_animator) _animator.SetBool(_animIDAim, true);
            _ce.weapons[3].SetActive(true);
            // 필요 시 활 전용 오브젝트 토글 등 CE/프리팹 설계에 맞게 추가
            pm.Local_RequestAim();
        }

        void EndAiming()
        {
            isAiming = false;
            if (_animator)
            {
                _animator.SetBool(_animIDAim, false);
                _animator.SetBool(_animIDPullString, false);
            }

            _ce.weapons[3].SetActive(false);
            // 활 보조 오브젝트 끄기 등
        }

        void AttackWand(Weapon active)
        {
            if (_input.attackPressed)
            {
                Transform spine = _animator.GetBoneTransform(HumanBodyBones.Spine);
                pm.Local_RequestAim();
                pressStartTime = Time.time;
                isAiming = true;
                if (_animator) _animator.SetBool(_animIDMagicAim, true);
                _input.attackPressed = false;
                _ce.weapons[4].SetActive(true);
            }

            if (isAiming)
            {
                bool charging = _input.attackHeld;
                if (_animator) _animator.SetBool(_animIDMagicCharge, charging);
            }

            if (_input.attackReleased)
            {
                float cd = Time.time - pressStartTime;
                //놓는 순간 shoot bow broadcast
                if (isAiming && isFireReady && cd >= active._wandMinChargeTime)
                {
                    if (_animator) _animator.SetTrigger(_animIDMagicFire);
                    WeaponContext context = new WeaponContext
                    {
                        cameraTransform = _aimOrigin,
                        chargeDuration = cd,
                        shooterId = -1
                    };
                    active.Use(context);   // 실제 발사
                    fireDelay = 0f;
                    pm.Local_RequestShoot(_aimOrigin, cd);
                }

                isAiming = false;
                if (_animator)
                {
                    _animator.SetBool(_animIDMagicAim, false);
                    _animator.SetBool(_animIDMagicCharge, false);
                }
                _input.attackReleased = false;
                _ce.weapons[4].SetActive(false);
            }
        }

        
    }
}