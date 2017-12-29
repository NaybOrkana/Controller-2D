using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour 
{
	public Controller2D m_Target;
	public Vector2 m_FocusAreaSize;

	public float m_VerticalOffset;
	public float m_LookAheadDistanceX;
	public float m_LookSmoothTimeX;
	public float m_VerticalSmoothTime;

	private FocusArea m_FocusArea;

	private float m_CurrentLookAheadX;
	private float m_TargetLookAheadX;
	private float m_LookAheadDirectionX;
	private float m_SmoothLookVelocityX;
	private float m_SmoothVelocityY;

	private bool m_LookAheadStopped;

	private void Start()
	{
		m_FocusArea = new FocusArea (m_Target.GetComponent<BoxCollider2D>().bounds, m_FocusAreaSize);
	}

	private void LateUpdate()
	{
		m_FocusArea.Update (m_Target.GetComponent<BoxCollider2D>().bounds);

		Vector2 focusPosition = m_FocusArea.centre + Vector2.up * m_VerticalOffset;

		if (m_FocusArea.velocity.x != 0) 
		{
			m_LookAheadDirectionX = Mathf.Sign (m_FocusArea.velocity.x);

			if (Mathf.Sign (m_Target.m_PlayerInput.x) == Mathf.Sign (m_FocusArea.velocity.x) && m_Target.m_PlayerInput.x != 0) 
			{
				m_LookAheadStopped = false;

				m_TargetLookAheadX = m_LookAheadDirectionX * m_LookAheadDistanceX;

			} 
			else 
			{
				if (!m_LookAheadStopped) 
				{
					m_LookAheadStopped = true;
					m_TargetLookAheadX = m_CurrentLookAheadX + (m_LookAheadDirectionX * m_LookAheadDistanceX - m_CurrentLookAheadX) / 4f;
				}
			}
		}
			 

		m_CurrentLookAheadX = Mathf.SmoothDamp (m_CurrentLookAheadX, m_TargetLookAheadX, ref m_SmoothLookVelocityX, m_LookSmoothTimeX);

		focusPosition.y = Mathf.SmoothDamp (transform.position.y, focusPosition.y, ref m_SmoothVelocityY, m_VerticalSmoothTime);
		focusPosition += Vector2.right * m_CurrentLookAheadX;

		transform.position = (Vector3)focusPosition + Vector3.forward * -10f;
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color (1, 0, 0, 0.5f);
		Gizmos.DrawCube (m_FocusArea.centre, m_FocusAreaSize);
	}

	struct FocusArea
	{
		public Vector2 centre;
		public Vector2 velocity;
		float top, bottom;
		float left, right;

		public FocusArea (Bounds targetBounds, Vector2 size)
		{
			left = targetBounds.center.x - size.x/2; 
			right = targetBounds.center.x + size.x/2;
			top = targetBounds.min.y + size.y;
			bottom = targetBounds.min.y;

			velocity = Vector2.zero;

			centre = new Vector2((left + right)/2, (top + bottom) / 2);
		}

		public void Update(Bounds targetBounds)
		{
			float shiftX = 0;

			if (targetBounds.min.x < left) {
				shiftX = targetBounds.min.x - left;
			}
			else if (targetBounds.max.x > right)
			{
				shiftX = targetBounds.max.x - right;
			}

			left += shiftX;
			right += shiftX;

			float shiftY = 0;

			if (targetBounds.min.y < bottom) 
			{
				shiftY = targetBounds.min.y - bottom;

			}
			else if (targetBounds.max.y > top) 
			{
				shiftY = targetBounds.max.y - top;
			}

			top += shiftY;
			bottom += shiftY;

			centre = new Vector2((left + right) / 2f, (top + bottom) / 2f);
			velocity = new Vector2 (shiftX, shiftY);
		}
	}

}
