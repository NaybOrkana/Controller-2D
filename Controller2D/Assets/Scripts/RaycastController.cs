using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (BoxCollider2D))]
public class RaycastController : MonoBehaviour 
{
	public const float m_SkinWidth = 0.015f;

	public LayerMask m_CollisionMask;
	const float m_DistanceBetweenRays = 0.25f;
	[HideInInspector]
	public int m_HorizontalRayCount;
	[HideInInspector]
	public int m_VerticalRayCount;

	[HideInInspector]
	public float m_HorizontalRaySpacing;
	[HideInInspector]
	public float m_VerticalRaySpacing;

	[HideInInspector]
	public BoxCollider2D m_BoxCollider;
	[HideInInspector]
	public RaycastOrigins m_RaycastOrigins;

	private void Awake()
	{
		m_BoxCollider = GetComponent<BoxCollider2D> ();		
	}

	private void Start()
	{
		CalculateRaySpacing ();
	}

	public void UpdateRaycastOrigins()
	{
		// The origins of the Raycast are inside as to prevent faulty detections and other common errors.
		Bounds bounds = m_BoxCollider.bounds;
		bounds.Expand (m_SkinWidth * -2);

		m_RaycastOrigins.bottomLeft = new Vector2 (bounds.min.x, bounds.min.y);
		m_RaycastOrigins.bottomRight = new Vector2 (bounds.max.x, bounds.min.y);
		m_RaycastOrigins.topLeft = new Vector2 (bounds.min.x, bounds.max.y);
		m_RaycastOrigins.TopRight = new Vector2 (bounds.max.x, bounds.max.y);
	}

	public void CalculateRaySpacing()
	{
		// This just spaces out the rays between them.
		Bounds bounds = m_BoxCollider.bounds;
		bounds.Expand (m_SkinWidth * -2);

		float boundsWidth = bounds.size.x;
		float boundsHeight = bounds.size.y;

		m_HorizontalRayCount = Mathf.RoundToInt (boundsHeight / m_DistanceBetweenRays);
		m_VerticalRayCount = Mathf.RoundToInt (boundsWidth / m_DistanceBetweenRays);

		m_HorizontalRaySpacing = bounds.size.y / (m_HorizontalRayCount - 1);
		m_VerticalRaySpacing = bounds.size.x / (m_VerticalRayCount - 1);
	}


	public struct RaycastOrigins
	{
		// This struct stores a bound inside the player, where the raycasts are going to fired from
		public Vector2 bottomLeft, bottomRight;
		public Vector2 topLeft, TopRight;
	}
}
