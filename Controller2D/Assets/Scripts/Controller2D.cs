using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (RaycastController))]
public class Controller2D : MonoBehaviour 
{
	public float m_MaxSlopeAngle = 80f;

	public CollisionInfo m_CollisionInfo;

	[HideInInspector]
	public RaycastController m_RaycastController;
	[HideInInspector]
	public Vector2 m_PlayerInput;

	private void Awake()
	{
		m_RaycastController = GetComponent<RaycastController> ();
	}

	private void Start()
	{
		// By standard the player is always facing right at first.
		m_CollisionInfo.FaceDirection = 1;
	}

	public void Move(Vector2 velocity, bool standingOnPlatform)
	{
		Move (velocity, Vector2.zero, standingOnPlatform);
	}

	public void Move(Vector2 velocity, Vector2 input, bool standingOnPlatform = false)
	{
		m_RaycastController.UpdateRaycastOrigins ();

		m_CollisionInfo.Reset ();
		m_CollisionInfo.velocityOld = velocity;

		m_PlayerInput = input;

		if (velocity.y < 0) // If the player is on the ground but its Y movement decreases, that means it is in a descending slope. 
		{
			DescendSlope (ref velocity);
		}

		if (velocity.x != 0) // therefore the direction it faces may be changed.
		{
			m_CollisionInfo.FaceDirection = (int)Mathf.Sign (velocity.x);
		}

		// The Controller should always check for Horizontal collisions, that way it can know if it is close enough to a wall, and therefore be able to wall slide and wall jump.
		HorizontalCollisions (ref velocity);

		// If not on the ground, either ascending or descending the player checks collisions.
		if (velocity.y != 0) 
		{
			VerticalCollisions (ref velocity);
		}

		transform.Translate (velocity);

		if (standingOnPlatform) 
		{
			m_CollisionInfo.below = true;
		}
	}

	private void HorizontalCollisions(ref Vector2 velocity)
	{
		float directionX = m_CollisionInfo.FaceDirection;
		float rayLength = Mathf.Abs (velocity.x) + RaycastController.m_SkinWidth;

		if (Mathf.Abs (velocity.x) < RaycastController.m_SkinWidth) 
		{
			rayLength = 2 * RaycastController.m_SkinWidth;
		}

		for (int i = 0; i < m_RaycastController.m_HorizontalRayCount; i++) 
		{
			// This is the same as writing an if statement (directionY == -1) {rayOrigin = bottomLeft}, else {rayOrigin = bottomRight}.
			// If the direction is negative we are moving left, otherwise, we are moving right.
			// Multiple rays can be casted from the player as well, with a constant value of RaySpacing between them.
			Vector2 rayOrigin = (directionX == -1) ? m_RaycastController.m_RaycastOrigins.bottomLeft : m_RaycastController.m_RaycastOrigins.bottomRight;
			rayOrigin += Vector2.up * (m_RaycastController.m_HorizontalRaySpacing * i);

			RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.right * directionX, rayLength, m_RaycastController.m_CollisionMask);

			Debug.DrawRay (rayOrigin, Vector2.right * directionX, Color.red);

			if (hit)
			{
				if (hit.distance == 0) 
				{
					continue;
				}
				// The angle is that between the normal vector of the hit (a vector going perpendicular to it i.e. 90 degrees) and the vector up.
				// This gets called even against walls, wall having an 90 degrees angle, if the MaxClimbAngle is set to >90, it can climb walls.
				float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);

				// If we can climb the slope, the ray distance would prevent the player from touching the surface of the slope.
				// It needs to be updated as to be able to properly sit in the slope and after climbing said slope the distance and velocity return to normal.
				if (i == 0 && slopeAngle <= m_MaxSlopeAngle) 
				{
					//In the scenario that the player climbs down a slope and immediately starts climbing one; the velocity x must be return to what it previously was as the player no longer descends a slope.
					if (m_CollisionInfo.descendingSlope) 
					{
						m_CollisionInfo.descendingSlope = false;
						velocity = m_CollisionInfo.velocityOld;
					}

					float distanceToSlopeStart = 0;
					if (slopeAngle != m_CollisionInfo.slopeAngleOld) 
					{
						distanceToSlopeStart = hit.distance - RaycastController.m_SkinWidth;
						velocity.x -= distanceToSlopeStart * directionX;
					}

					ClimbSlope (ref velocity, slopeAngle, hit.normal);
					velocity.x += distanceToSlopeStart * directionX;
				}

				// If we are not in a slope, they ray gets casted with its full size and the distance and velocity are not updated.
				if (!m_CollisionInfo.climbingSlopes || slopeAngle > m_MaxSlopeAngle) 
				{
					velocity.x = (hit.distance - RaycastController.m_SkinWidth) * directionX;
					rayLength = hit.distance;

					// When we climb a slope, the velocity.Y does not get updated, meaning that if it encounters an obstacle in the way, it'll get stuck.
					// Because the script does not know right now what is the moveDistance, the Tan function of our stored slopeAngle can be used to get it and update the Y velocity in proportion of velocity.X.
					if (m_CollisionInfo.climbingSlopes) {
						velocity.y = Mathf.Tan (m_CollisionInfo.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs (velocity.x);
					}

					// This is also the same as an if statement, in this case if (directionX == -1) {m_CollisionInfo.left = directionX}
					m_CollisionInfo.left = directionX == -1;
					m_CollisionInfo.right = directionX == 1;
				}
			}
		}
	}


	private void VerticalCollisions(ref Vector2 velocity)
	{
		float directionY = Mathf.Sign (velocity.y);
		float rayLength = Mathf.Abs (velocity.y) + RaycastController.m_SkinWidth;

		for (int i = 0; i < m_RaycastController.m_VerticalRayCount; i++) 
		{
			// This is the same as writing an if statement (directionY == -1) = bottomLeft, else = topLeft.
			Vector2 rayOrigin = (directionY == -1) ? m_RaycastController.m_RaycastOrigins.bottomLeft : m_RaycastController.m_RaycastOrigins.topLeft;
			rayOrigin += Vector2.right * (m_RaycastController.m_VerticalRaySpacing * i + velocity.x);

			RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.up * directionY, rayLength, m_RaycastController.m_CollisionMask);

			Debug.DrawRay (rayOrigin, Vector2.up * directionY, Color.red);

			// If we are climbing a slope and the player encounters and obstacle above it and collides with it, it'll get stuck.
			// So velocity.x also needs to be updated in proportion of velocity.y when this value changes.
			if (hit) 
			{
				// This is another way of falling through platforms, current way stops for a fraction of a second in each platform while descending, while this one goes through like there's nothing.
//				if (hit.collider == "Through")
//				{
//					if (directionY == 1 || hit.distance == 0) 
//					{
//						continue;
//					}
//					if (m_CollisionInfo.fallingThroughPlatform) 
//					{
//						continue;
//					}
//					if (m_PlayerInput.y = -1) 
//					{
//						m_CollisionInfo.fallingThroughPlatform = true;
//						Invoke ("ResetFalling", 0.1f);
//					}
//				}

				if (m_CollisionInfo.fallingThroughPlatform != null) 
				{
					if (m_CollisionInfo.fallingThroughPlatform == hit.collider) 
				{
						continue;
				} 
					else 
					{
							m_CollisionInfo.fallingThroughPlatform = null;
					}
				}
					
				if (hit.collider.tag == "Through") 
				{
					if (directionY == 1 || hit.distance == 0) 
					{
						continue;
					}

					if (m_PlayerInput.y == -1)
					{
						m_CollisionInfo.fallingThroughPlatform = hit.collider;
					}
				}

				velocity.y = (hit.distance - RaycastController.m_SkinWidth) * directionY;
				rayLength = hit.distance;

				if (m_CollisionInfo.climbingSlopes) 
				{
					velocity.x = velocity.y / Mathf.Tan (m_CollisionInfo.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign (velocity.x);
				}

				m_CollisionInfo.below = directionY == -1;
				m_CollisionInfo.above = directionY == 1;
			}
		}


		if (m_CollisionInfo.climbingSlopes)
		{
			// Adjusts the ray casting if the player is on a slope.
			float directionX = Mathf.Sign (velocity.x);
			rayLength = Mathf.Abs (velocity.x) + RaycastController.m_SkinWidth;
			Vector2 rayOrigin = ((directionX == -1) ? m_RaycastController.m_RaycastOrigins.bottomLeft : m_RaycastController.m_RaycastOrigins.bottomRight) + Vector2.up * velocity.y;
			RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.right * directionX, rayLength, m_RaycastController.m_CollisionMask);

			if (hit) 
			{
				float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);

				if (slopeAngle != m_CollisionInfo.slopeAngle)
				{
					velocity.x = (hit.distance - RaycastController.m_SkinWidth) * directionX;
					m_CollisionInfo.slopeAngle = slopeAngle;
					m_CollisionInfo.slopeNormal = hit.normal;
				}
			}
		}
	}

	private void ClimbSlope(ref Vector2 velocity, float slopeAngle, Vector2 slopeNormal)
	{
		// When the raycasts get a hit, they evaluate the distance of its ray. That is lenght of the slope and also the hypotenuse of the triangle it forms.
		float moveDistance = Mathf.Abs (velocity.x);
		float climbVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;

		// If the player has not climbed the slope.
		// Knowing the angle we need the unknown sides of the triangle. We get them via their Sin and Cos functions, multiplied by the distance.
		// The Horizontal velocity gets ajusted to the slope angle as to be able to climb it without disrupting the maintained speed. (This of course is adjustable by design, the player can run slopes slower or faster as well).
		if (velocity.y <= climbVelocityY) 
		{
			velocity.y = climbVelocityY;
			velocity.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);

			m_CollisionInfo.below = true;
			m_CollisionInfo.climbingSlopes = true;
			m_CollisionInfo.slopeAngle = slopeAngle;
			m_CollisionInfo.slopeNormal = slopeNormal;
		}
	}

	private void DescendSlope(ref Vector2 velocity)
	{
		// Using both sides of the player to determine if a slope is traversable or not and avoi jittering, as well as aiding the actual sliding.
		RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast (m_RaycastController.m_RaycastOrigins.bottomLeft, Vector2.down, Mathf.Abs (velocity.y) + RaycastController.m_SkinWidth, m_RaycastController.m_CollisionMask);
		RaycastHit2D maxSlopeHitRight = Physics2D.Raycast (m_RaycastController.m_RaycastOrigins.bottomRight, Vector2.down, Mathf.Abs (velocity.y) + RaycastController.m_SkinWidth, m_RaycastController.m_CollisionMask);

		if (maxSlopeHitLeft ^ maxSlopeHitRight) //If either cast has a hit the player is going to slide down, only when the conditions are met, but it does not need to be completely on the slope to start descending it.
		{
			SlideDownMaxSlope(maxSlopeHitLeft, ref velocity);
			SlideDownMaxSlope(maxSlopeHitRight, ref velocity);
		}
			

		float directionX = Mathf.Sign (velocity.x);
		Vector2 rayOrigin = (directionX == -1) ? m_RaycastController.m_RaycastOrigins.bottomRight : m_RaycastController.m_RaycastOrigins.bottomLeft;
		RaycastHit2D hit = Physics2D.Raycast (rayOrigin, -Vector2.up, Mathf.Infinity, m_RaycastController.m_CollisionMask);

		if (!m_CollisionInfo.slidingDownMaxSlope) 
		{
			if (hit)
			{
				float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);

				if (slopeAngle != 0 && slopeAngle < m_MaxSlopeAngle) 
				{
					if (Mathf.Sign (hit.normal.x) == directionX) 
					{
						if (hit.distance - RaycastController.m_SkinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x)) 
						{
							float moveDistance = Mathf.Abs (velocity.x);
							float descendVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;

							velocity.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign (velocity.x);
							velocity.y -= descendVelocityY;

							m_CollisionInfo.slopeAngle = slopeAngle;
							m_CollisionInfo.descendingSlope = true;
							m_CollisionInfo.below = true;
							m_CollisionInfo.slopeNormal = hit.normal;

						}
					}
				}
			}
		}
	}

	private void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 velocity)
	{
		if (hit) 
		{
			float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);

			// If the slope has too much of an angle, the player will slide on it, the velocity needed is known by the triangle the slope forms with its normal and the x position of the player.
			// And we know this values via the angle so: X = Y - Hit Distance (cause the player may still be on the air when the ray hits) / Tan of the angle.
			if (slopeAngle > m_MaxSlopeAngle)
			{
				velocity.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs (velocity.y) - hit.distance) / Mathf.Tan (slopeAngle * Mathf.Deg2Rad);

				m_CollisionInfo.slopeAngle = slopeAngle;
				m_CollisionInfo.slidingDownMaxSlope = true;
				m_CollisionInfo.slopeNormal = hit.normal;
			}
		}
	}

//	private void ResetFalling()
//	{
//		m_CollisionInfo.fallingThroughPlatform = false;
//	}



	public struct CollisionInfo
	{
		public bool above, below;
		public bool left, right;

		public bool climbingSlopes;
		public bool descendingSlope;
		public bool slidingDownMaxSlope;

		public Collider2D fallingThroughPlatform;

		public float slopeAngle, slopeAngleOld;
		public Vector2 slopeNormal;
		public Vector2 velocityOld;
		public int FaceDirection;

		public void Reset()
		{
			above = below = false;
			left = right = false;
			climbingSlopes = false;
			descendingSlope = false;
			slidingDownMaxSlope = false;
			slopeNormal = Vector2.zero;

			slopeAngleOld = slopeAngle;
			slopeAngle = 0;
		}
	}
}
