using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
        // Trigger 용으로 단발 입력 처리
        [HideInInspector] public bool rollPressed;
		public bool interaction;
		public bool attack;

        public bool attackPressed;   // 눌린 순간
        public bool attackHeld;      // 누르고 있는 중
        public bool attackReleased = false;  // 뗀 순간
        private bool attackWasHeld = false; // 이전 프레임의 상태 저장

		public bool drop;

        [Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = false;
		public bool cursorInputForLook = false;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
        public void OnRoll(InputValue value)
        {
            RollInput(value.isPressed);
        }
		public void OnInteraction(InputValue value)	
		{
            InteractionInput(value.isPressed);
        }
        public void OnAttack(InputValue value)
        {
			AttackInput(value);
        }
		public void OnDrop(InputValue value)
		{
			// DropInput(value.isPressed);
			DropInput(value.isPressed);
        }
#endif


        public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
        }

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}
        public void RollInput(bool newRollState)
        {
            rollPressed = newRollState;
        }
        public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		public void InteractionInput(bool newInteractionState)
		{
			
                interaction = newInteractionState;
        }
		public void AttackInput(InputValue value)
		{
            // Attack input handling can be added here if needed
            bool isPressed = value.isPressed;

            if (isPressed)
            {
                // 눌린 순간만 true
                if (!attackWasHeld)
                {
                    attackPressed = true;
                }
                else
                {
                    attackPressed = false;
                }

                attackHeld = true;
                attackReleased = false;
            }
            else
            {
                attackPressed = false;
                attackHeld = false;

                if (attackWasHeld)
                {
                    attackReleased = true; // 뗀 순간만 true
                }
                else
                {
                    attackReleased = false;
                }
            }

            attackWasHeld = isPressed;
        }
		public void DropInput(bool newDropState) { 
			drop = newDropState;
        }

        private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}        
    }
}