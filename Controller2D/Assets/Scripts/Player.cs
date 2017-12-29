using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour 
{
	public float m_MoveSpeed = 6f;
	public float m_MaxJumpHeight = 4f;
	public float m_MinJumpHeight = 1f;
	public float m_WallSlideSpeed = 3f; 
	public Vector2 m_WallJumpClimbing;
	public Vector2 m_WallJumpOff;
	public Vector2 m_WallLeap;

	public float m_TimeToJumpApex = 0.4f;
	public float m_AccelerationTimeAirborne = 0.2f;
	public float m_AccelerationTimeGrounded = 0.1f;
	public float m_WallStickTime = 0.25f;

	private bool m_WallSliding = false;
	private float m_TimeToWallUnstick;
	private float m_MaxJumpVelocity;
	private float m_MinJumpVelocity;
	private float m_Gravity;
	private float m_VelocityXSmoothing;
	private Vector3 m_Velocity;
	private Controller2D m_Controller;
	private Vector2 m_DirectionalInput;
	private int m_WallDirX;

	private void Start()
	{
		m_Controller = GetComponent<Controller2D> ();

		m_Gravity = -(2 * m_MaxJumpHeight) / Mathf.Pow (m_TimeToJumpApex, 2f);
		m_MaxJumpVelocity = Mathf.Abs (m_Gravity) * m_TimeToJumpApex;
		m_MinJumpVelocity = Mathf.Sqrt (2 * Mathf.Abs (m_Gravity) * m_MinJumpHeight);

		Debug.Log ("Gravity: " + m_Gravity + " Jump velocity: " + m_MaxJumpVelocity);
	}

	private void Update()
	{
		CalculateVelocity ();

		HandleWallSliding ();

		m_Controller.Move (m_Velocity * Time.deltaTime, m_DirectionalInput);

		if (m_Controller.m_CollisionInfo.above || m_Controller.m_CollisionInfo.below) 
		{
			if (m_Controller.m_CollisionInfo.slidingDownMaxSlope) // If the player is in a not traversable slope, it will slide down.
			{
				m_Velocity.y += m_Controller.m_CollisionInfo.slopeNormal.y * -m_Gravity * Time.deltaTime;
			}
			else // Otherwise, it stays still.
			{
				m_Velocity.y = 0;
			}
		}
	}

	public void SetDirectionalInput(Vector2 input)
	{
		m_DirectionalInput = input;
	}

	public void OnJumpInputDown()
	{
		if (m_WallSliding) 
		{
			// The player wall climbs Megaman-Style if jumping against a wall and it is holding the same direction as it is touching the wall.
			if (m_WallDirX == m_DirectionalInput.x) 
			{
				m_Velocity.x = -m_WallDirX * m_WallJumpClimbing.x;
				m_Velocity.y = m_WallJumpClimbing.y;
			}

			// The player will kick itself out of the wall if the jump button is pressed while sliding but no direction is held.
			else if (m_DirectionalInput.x == 0)
			{
				m_Velocity.x = -m_WallDirX * m_WallJumpOff.x;
				m_Velocity.y = m_WallJumpOff.y;
			}

			// If the player is holding the other direction while wall sliding it will perform a wall leap Mario-Style
			else 
			{
				m_Velocity.x = -m_WallDirX * m_WallLeap.x;
				m_Velocity.y = m_WallLeap.y;
			}
		}

		// Normal jumping on the ground
		if (m_Controller.m_CollisionInfo.below)
		{
			if (m_Controller.m_CollisionInfo.slidingDownMaxSlope) 
			{
				if (m_DirectionalInput.x != -Mathf.Sign(m_Controller.m_CollisionInfo.slopeNormal.x)) //If not jumping against a max slope.
				{
					// Then we jump in an arc with the direction of the slope.
					m_Velocity.y = m_MaxJumpVelocity * m_Controller.m_CollisionInfo.slopeNormal.y;
					m_Velocity.x = m_MaxJumpVelocity * m_Controller.m_CollisionInfo.slopeNormal.x;
				}
			} 
			else 
			{
				m_Velocity.y = m_MaxJumpVelocity;
			}
		}
	}

	public void OnJumpInputUp()
	{
		if (m_Velocity.y > m_MinJumpVelocity)
		{
			m_Velocity.y = m_MinJumpVelocity;
		}
	}
		
	private void HandleWallSliding()
	{
		// The direction of the wall is stored, this is needed for wall jumping cases of leap, climb and kick off.
		m_WallDirX = (m_Controller.m_CollisionInfo.left) ? -1 : 1;

		m_WallSliding = false;
		// If we are touching either left or right, the player is not on the ground AND it is falling, then it is wall sliding.
		if ((m_Controller.m_CollisionInfo.left || m_Controller.m_CollisionInfo.right) && !m_Controller.m_CollisionInfo.below && m_Velocity.y < 0) 
		{
			m_WallSliding = true;

			if (m_Velocity.y < -m_WallSlideSpeed) 
			{
				m_Velocity.y = -m_WallSlideSpeed;
			}

			// This is a countdown for the other directional input to be more manageable while Wall Leaping, otherwise the player would unstick from the wall in an instant.
			if (m_TimeToWallUnstick > 0)
			{
				m_VelocityXSmoothing = 0;
				m_Velocity.x = 0;

				if (m_DirectionalInput.x != m_WallDirX && m_DirectionalInput.x != 0) 
				{
					m_TimeToWallUnstick -= Time.deltaTime;
				}
				else 
				{
					m_TimeToWallUnstick = m_WallStickTime;
				}
			} 
			else
			{
				m_TimeToWallUnstick = m_WallStickTime;

			}
		}
	}

	private void CalculateVelocity()
	{
		// Calculates speed from zero to our desired speed by gradually increasing instead of a sudden shift.
		float TargetVelocityX = m_DirectionalInput.x * m_MoveSpeed;
		m_Velocity.x = Mathf.SmoothDamp (m_Velocity.x, TargetVelocityX, ref m_VelocityXSmoothing, (m_Controller.m_CollisionInfo.below) ? m_AccelerationTimeGrounded : m_AccelerationTimeAirborne);
		m_Velocity.y += m_Gravity * Time.deltaTime;
	}
}
