using Klei.AI;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.DuplicantActions;
using UnityEngine;

namespace ONI_MP.Networking.Synchronization
{
	// Attached to minions on the Host side.
	// Periodically checks if vitals have changed significantly and sends updates.
	public class VitalStatsSyncer : MonoBehaviour
	{
		private NetworkIdentity _identity;
		private Amounts _amounts;
		private float _lastSendTime;
		private const float SYNC_INTERVAL = 1.0f; // 1 second interval

		// Cached last values to detect changes
		private float _lastHealth;
		private float _lastCalories;
		private float _lastStress;
		private float _lastBreath;
		private float _lastStamina;
		private float _lastBladder;

		private void Awake()
		{
			_identity = GetComponent<NetworkIdentity>();
			_amounts = GetComponent<Amounts>();
		}

		private void Update()
		{
			if (!MultiplayerSession.IsHost) return;
			if (_identity == null || _amounts == null) return;
			if (Time.time - _lastSendTime < SYNC_INTERVAL) return;

			// Gather current values
			float health = GetValue("HitPoints");
			float calories = GetValue("Calories");
			float stress = GetValue("Stress");
			float breath = GetValue("Breath");
			float stamina = GetValue("Stamina");
			float bladder = GetValue("Bladder");

			// Check diffs (arbitrary thresholds)
			bool changed =
					Mathf.Abs(health - _lastHealth) > 0.5f ||
					Mathf.Abs(calories - _lastCalories) > 50f || // Increased threshold for calories
					Mathf.Abs(stress - _lastStress) > 1f ||
					Mathf.Abs(breath - _lastBreath) > 5f ||
					Mathf.Abs(stamina - _lastStamina) > 5f ||
					Mathf.Abs(bladder - _lastBladder) > 5f;

			// TODO: Also check germs diff?

			if (changed || Time.time - _lastSendTime > 10f) // Force update every 10s
			{
				var pe = _identity.GetComponent<PrimaryElement>();
				float maxCal = _amounts.Get("Calories")?.GetMax() ?? 0;

				var packet = new VitalStatsPacket
				{
					NetId = _identity.NetId,
					Health = health,
					Calories = calories,
					MaxCalories = maxCal,
					Stress = stress,
					Breath = breath,
					Stamina = stamina,
					Bladder = bladder,
					GermElemIdx = pe?.DiseaseIdx ?? 255,
					GermCount = pe?.DiseaseCount ?? 0
				};

				PacketSender.SendToAllClients(packet);

				_lastHealth = health;
				_lastCalories = calories;
				_lastStress = stress;
				_lastBreath = breath;
				_lastStamina = stamina;
				_lastBladder = bladder;

				_lastSendTime = Time.time;
			}
		}

		private float GetValue(string id)
		{
			var ai = _amounts.Get(id);
			return ai != null ? ai.value : 0f;
		}
	}
}
