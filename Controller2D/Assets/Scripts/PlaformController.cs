using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaformController : MonoBehaviour 
{
	public LayerMask m_PassengerMask;

	public Vector3[] m_LocalWaypoints;
	private Vector3 [] m_GlobalWaypoints;

	public float m_Speed;
	public bool m_IsCyclic;
	public float m_WaitTime;
	[Range (0, 2)]
	public float m_EaseAmount;

	private int m_FromWaypointIndex;
	private float m_PercentBetweenWaypoints;
	private float m_NextMoveTime;

	private RaycastController m_RaycastController;

	private List<PassengerMovement> m_PassengerMovement;
	private Dictionary<Transform, Controller2D> m_PassengerDictionary = new Dictionary<Transform, Controller2D> ();

	private void Start()
	{
		m_RaycastController = GetComponent<RaycastController> ();

		m_GlobalWaypoints = new Vector3[m_LocalWaypoints.Length];

		for (int i = 0; i < m_LocalWaypoints.Length; i++) 
		{
			m_GlobalWaypoints [i] = m_LocalWaypoints [i] + transform.position;
		}
	}

	private void Update()
	{
		m_RaycastController.UpdateRaycastOrigins ();

		Vector3 velocity = CalculatePlatformMovement ();

		CalculatePassengerMovement (velocity);

		MovePassengers (true);
		transform.Translate (velocity);
		MovePassengers (false);
	}

	private float Ease(float easedValue)
	{
		float easing = m_EaseAmount + 1;

		return Mathf.Pow (easedValue, easing) / (Mathf.Pow (easedValue, easing) + Mathf.Pow (1 - easedValue, easing));
	}

	private Vector3 CalculatePlatformMovement()
	{
		if (Time.time < m_NextMoveTime) {
			return Vector3.zero;
		}

		//A little bit more complex, this takes every waypoint in its order, stores it, then it calculates the distance between current position and the waypoint position and ease the movement between the two.

		m_FromWaypointIndex %= m_GlobalWaypoints.Length;
		int toWaypointIndex = (m_FromWaypointIndex + 1) % m_GlobalWaypoints.Length;
		float distanceBetweenWaypoints = Vector3.Distance (m_GlobalWaypoints [m_FromWaypointIndex], m_GlobalWaypoints [toWaypointIndex]);
		m_PercentBetweenWaypoints += Time.deltaTime * m_Speed / distanceBetweenWaypoints;
		m_PercentBetweenWaypoints = Mathf.Clamp01 (m_PercentBetweenWaypoints);
		float easedPercentBetweenWaypoints = Ease (m_PercentBetweenWaypoints);

		Vector3 newPos = Vector3.Lerp (m_GlobalWaypoints [m_FromWaypointIndex], m_GlobalWaypoints [toWaypointIndex], easedPercentBetweenWaypoints);

		if (m_PercentBetweenWaypoints >= 1) 
		{
			m_PercentBetweenWaypoints = 0;
			m_FromWaypointIndex++;

			if (!m_IsCyclic) {
				if (m_FromWaypointIndex >= m_GlobalWaypoints.Length - 1)
				{
					m_FromWaypointIndex = 0;
					System.Array.Reverse (m_GlobalWaypoints);
				}
			}

			m_NextMoveTime = Time.time + m_WaitTime;
		}

		return newPos - transform.position;
	}

	private void MovePassengers(bool beforeMovePlatform)
	{
		// This adds the player to the platform as to not fall down while moving and stores the data from the passenger as to ease processing,
		// i.e. It does not makes this all the time, just once per platform, per passenger.

		foreach (PassengerMovement passenger in m_PassengerMovement)
		{
			if (!m_PassengerDictionary.ContainsKey(passenger.transform)) {
				m_PassengerDictionary.Add (passenger.transform, passenger.transform.GetComponent<Controller2D> ());
			}

			if (passenger.moveBeforePlatform == beforeMovePlatform) {
				m_PassengerDictionary [passenger.transform].Move (passenger.velocity, passenger.standingOnPlatform);
			}
		}
	}

	private void CalculatePassengerMovement(Vector3 velocity)
	{
		HashSet<Transform> movedPassengers = new HashSet<Transform> ();
		m_PassengerMovement = new List<PassengerMovement> ();

		float directionX = Mathf.Sign (velocity.x);
		float directionY = Mathf.Sign (velocity.y);

		// Vertically moving platforms
		if (velocity.y != 0) {
			float rayLength = Mathf.Abs (velocity.y) + RaycastController.m_SkinWidth;

			//The rays are being casted from the platform outwards if the hit a passenger then it adds it to it's list and the passenger's position and velocity now mirrors the platform.
			// A tad more complex, but it is copy-pastable.
			for (int i = 0; i < m_RaycastController.m_VerticalRayCount; i++) {
				Vector2 rayOrigin = (directionY == -1) ? m_RaycastController.m_RaycastOrigins.bottomLeft : m_RaycastController.m_RaycastOrigins.topLeft;
				rayOrigin += Vector2.right * (m_RaycastController.m_VerticalRaySpacing) * i;
				RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.up * directionY, rayLength, m_PassengerMask);

				if (hit && hit.distance != 0) {
					if (!movedPassengers.Contains(hit.transform)) {
						movedPassengers.Add (hit.transform);
						float pushX = (directionX == 1) ? velocity.x : 0;
						float pushY = velocity.y - (hit.distance - RaycastController.m_SkinWidth) * directionY;

						m_PassengerMovement.Add (new PassengerMovement (hit.transform, new Vector3 (pushX, pushY), directionY == 1, true));
					}
				}
			}
		}

		// Horizontally moving platforms
		if (velocity.x != 0) {
			float rayLength = Mathf.Abs (velocity.x) + RaycastController.m_SkinWidth;

			for (int i = 0; i < m_RaycastController.m_HorizontalRayCount; i++) {
				Vector2 rayOrigin = (directionX == -1) ? m_RaycastController.m_RaycastOrigins.bottomLeft : m_RaycastController.m_RaycastOrigins.bottomRight;
				rayOrigin += Vector2.up * (m_RaycastController.m_HorizontalRaySpacing) * i;
				RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.right * directionX, rayLength, m_PassengerMask);

				if (hit && hit.distance != 0) {
					if (!movedPassengers.Contains (hit.transform)) {
						movedPassengers.Add (hit.transform);
						float pushX = velocity.x - (hit.distance - RaycastController.m_SkinWidth) * directionX;
						float pushY = -RaycastController.m_SkinWidth;

						m_PassengerMovement.Add (new PassengerMovement (hit.transform, new Vector3 (pushX, pushY), false, true));
					}
				}
			}
		}

		//Passenger on top of a horizontally or downward moving platform
		if (directionY == -1 || velocity.y == 0 && velocity.x != 0) {
			float rayLength = RaycastController.m_SkinWidth * 2;

			for (int i = 0; i < m_RaycastController.m_VerticalRayCount; i++) {
				Vector2 rayOrigin = m_RaycastController.m_RaycastOrigins.topLeft + Vector2.right * (m_RaycastController.m_VerticalRaySpacing * i);
				RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.up, rayLength, m_PassengerMask);

				if (hit && hit.distance != 0) {
					if (!movedPassengers.Contains(hit.transform)) {
						movedPassengers.Add (hit.transform);
						float pushX = velocity.x;
						float pushY = velocity.y;

						m_PassengerMovement.Add (new PassengerMovement (hit.transform, new Vector3 (pushX, pushY), true, false));
					}
				}
			}
		}

	}

	struct PassengerMovement {
		public Transform transform;
		public Vector3 velocity;
		public bool standingOnPlatform;
		public bool moveBeforePlatform;

		public PassengerMovement(Transform _transform, Vector3 _velocity, bool _standingOnPlatform, bool _moveBeforePlatform) {
			transform = _transform;
			velocity = _velocity;
			standingOnPlatform = _standingOnPlatform;
			moveBeforePlatform = _moveBeforePlatform;
		}
	}

	private void OnDrawGizmos()
	{
		if (m_LocalWaypoints != null) {
			Gizmos.color = Color.red;
			float size = .3f;

			for (int i = 0; i < m_LocalWaypoints.Length; i++) {
				Vector3 globalWaypointpos = (Application.isPlaying) ? m_GlobalWaypoints [i] : m_LocalWaypoints [i] + transform.position;
				Gizmos.DrawLine (globalWaypointpos - Vector3.up * size, globalWaypointpos + Vector3.up * size);
				Gizmos.DrawLine (globalWaypointpos - Vector3.left * size, globalWaypointpos + Vector3.left * size);
			}
		}
	}
}
