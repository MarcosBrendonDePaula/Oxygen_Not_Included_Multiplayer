using UnityEngine;

namespace ONI_MP.Networking
{
    public static class NetIdHelper
    {
        /// <summary>
        /// Generates a deterministic NetID for a building based on its location and object layer.
        /// Range: 1,000,000,000+
        /// </summary>
        public static int GetDeterministicBuildingId(GameObject go)
        {
            if (go == null) return 0;

            int cell = Grid.PosToCell(go);
            if (!Grid.IsValidCell(cell)) return 0;

            var building = go.GetComponent<Building>();
            // Use ObjectLayer to distinguish between buildings, wires, pipes at the same cell
            int layer = (building != null) ? (int)building.Def.ObjectLayer : (int)ObjectLayer.Building;

            // Offset by 1 billion to avoid overlap with RNG-based IDs
            // ObjectLayer offset ensures different building types at the same spot get unique IDs
            return 1000000000 + cell + (layer * 1000000);
        }
    }
}
