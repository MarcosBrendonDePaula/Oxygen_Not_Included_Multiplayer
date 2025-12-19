namespace ONI_MP.Networking.Packets.Architecture
{
	public enum PacketType : int
	{
		ChatMessage = 1,
		EntityPosition = 2,
		ChoreAssignment = 3,    // Not in use atm
		WorldData = 4,          // Keeping for now, might find a use
		WorldDataRequest = 5,   // Keeping for now, might find a use
		WorldUpdate = 6,        // Batched world updates -  Not in use atm
		Instantiations = 7,     // Batched instantiations - Not in use atm
		NavigatorPath = 8,
		SaveFileRequest = 9,
		SaveFileChunk = 10,
		Diggable = 11,
		DigComplete = 12,
		PlayAnim = 13,
		Build = 14,
		BuildComplete = 15,
		WorldDamageSpawnResource = 16,
		WorldCycle = 17,
		Cancel = 18,
		Deconstruct = 19,
		DeconstructComplete = 20,
		WireBuild = 21,
		ToggleMinionEffect = 22,
		ToolEquip = 23,
		DuplicantCondition = 24,
		MoveToLocation = 25,     // Movement from the MoveTo tool
		Prioritize = 26,
		Clear = 27,               // Sweeping, Mopping etc
		ClientReadyStatus = 28,
		AllClientsReady = 29,
		ClientReadyStatusUpdate = 30,
		EventTriggered = 31,
		HardSync = 32,
		HardSyncComplete = 33, // Not in use atm
		Disinfect = 34,
		SpeedChange = 35,
		PlayerCursor = 36,
		// ID 37 is unused! (Replace this with something else)
		BuildingState = 38,
		DiggingState = 39,
		ChoreState = 40,
		ResearchState = 41,
		PrioritizeState = 42,
		DisinfectState = 43,
		DuplicantState = 44,
		StructureState = 45,
		ResearchRequest = 46,
		BuildingConfig = 47,
		ImmigrantOptions = 48,
		ImmigrantSelection = 49,
		DuplicantPriority = 50,
		SkillMastery = 51,
		ScheduleUpdate = 52,
		ScheduleAssignment = 53,
		FallingObject = 54,
		ConsumablePermission = 55,
		VitalStats = 56,
		ResourceCount = 57,
		Notification = 58,
		ScheduleDelete = 59,
		ConsumableState = 60,
		ResearchProgress = 61,
		ResearchComplete = 62,
		EntitySpawn = 63
	}
}

