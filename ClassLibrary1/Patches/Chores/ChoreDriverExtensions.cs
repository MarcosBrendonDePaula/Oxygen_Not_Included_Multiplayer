using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ONI_MP.Patches.Chores
{
    public static class ChoreAssignmentExtensions
    {
        public static void AssignChoreToDuplicant(this Chore newChore, GameObject dupeGO)
        {
            if (newChore == null || dupeGO == null)
            {
                Debug.LogWarning("[ChoreAssignment] Invalid chore or duplicant.");
                return;
            }

            var consumer = dupeGO.GetComponent<ChoreConsumer>();
            if (consumer == null || consumer.choreDriver == null)
            {
                Debug.LogWarning("[ChoreAssignment] Missing ChoreConsumer or ChoreDriver.");
                return;
            }

            var driver = consumer.choreDriver;

            // Cancel current chore
            var current = driver.GetCurrentChore();
            if (current != null)
            {
                current.Cancel("Override chore from MP");
            }

            // Build context and begin
            var state = new ChoreConsumerState(consumer);
            var context = new Chore.Precondition.Context(newChore, state, true);

            newChore.Begin(context);

            Debug.Log($"[ChoreAssignment] Assigned chore {newChore.id} to {dupeGO.name}");
        }
    }
}
