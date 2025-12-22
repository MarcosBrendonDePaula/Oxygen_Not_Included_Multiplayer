# ONI Game API Reference

This document contains reference information about Oxygen Not Included game classes and methods discovered during multiplayer mod development.

---

## Telepad / Immigration System

### `ImmigrantScreen` (UI class)
The main screen for selecting duplicants and care packages from the Printing Pod.

**Key Methods:**
| Method | Description |
|--------|-------------|
| `OnProceed()` | Called when player clicks "Print" to accept selection |
| `OnRejectAll()` | Called when player clicks "Reject All" button |
| `Deactivate()` | Closes the immigrant screen |
| `Initialize(Telepad telepad)` | Initializes the screen with a telepad reference |

**Key Fields (via Traverse):**
| Field | Type | Description |
|-------|------|-------------|
| `containers` | `List<ITelepadDeliverableContainer>` | List of container UI elements |
| `selectedDeliverables` | `List<ITelepadDeliverable>` | Currently selected deliverables |
| `selectedContainer` | Object | The currently selected container |
| `numberOfDuplicantOptions` | `int` | Max number of duplicant options |
| `numberOfCarePackageOptions` | `int` | Max number of care package options |

---

### `CharacterContainer` (UI class)
Container UI element for displaying a duplicant option.

**Key Fields (via Traverse):**
| Field | Type | Description |
|-------|------|-------------|
| `stats` | `MinionStartingStats` | The duplicant's starting stats |
| `animController` | KAnimController | Animation controller for display |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetInfo(MinionStartingStats stats)` | Sets the duplicant data to display |
| `SetAnimator()` | Updates the visual animation |
| `SetInfoText()` | Updates the text labels |
| `GenerateCharacter(bool is_starter)` | Generates a new random character |

---

### `CarePackageContainer` (UI class)
Container UI element for displaying a care package option.

**Key Fields (via Traverse):**
| Field | Type | Description |
|-------|------|-------------|
| `info` | `CarePackageInfo` | The care package data |
| `carePackageInstanceData` | `CarePackageInstanceData` | Instance-specific data |
| `entryIcons` | `List<GameObject>` | Visual icon GameObjects |
| `fgImage` | Image | Foreground image component |
| `animController` | KAnimController | Animation controller |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetAnimator()` | Updates the visual based on `info` field |
| `SetInfoText()` | Updates the text labels |
| `GenerateCharacter(bool is_starter)` | Generates content (NOT recommended for sync - regenerates random data) |

**Important Notes:**
- To update visuals, set `info` field first, then call `SetAnimator()` and `SetInfoText()`
- Do NOT call `GenerateCharacter()` for syncing - it regenerates random data
- Clear `entryIcons` (destroy GameObjects) before updating if icons are stale

---

### `MinionStartingStats`
Represents the starting stats for a new duplicant.

**Key Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Duplicant name |
| `personality` | `Personality` | Personality reference |
| `Traits` | `List<Trait>` | List of traits |

**Key Methods:**
| Method | Return | Description |
|--------|--------|-------------|
| `Deliver(Vector3 position)` | `GameObject` | Spawns the duplicant at position and returns the GameObject |

---

### `CarePackageInfo`
Represents care package data.

**Constructor:**
```csharp
new CarePackageInfo(string id, float quantity, System.Func<bool> requirement)
```

**Key Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `id` | `string` | Item prefab ID (e.g., "BasicForagePlant", "PuftEgg") |
| `quantity` | `float` | Amount of items |

**Key Methods:**
| Method | Return | Description |
|--------|--------|-------------|
| `Deliver(Vector3 position)` | `GameObject` | Spawns the care package items at position |

**Important Notes:**
- `Deliver()` triggers Telepad animation - may freeze on clients
- For client sync, use `Util.KInstantiate(Assets.GetPrefab(new Tag(itemId)), pos)` instead

---

### `Telepad`
The Printing Pod building component.

**Key Methods:**
| Method | Description |
|--------|-------------|
| `OnAcceptDelivery(ITelepadDeliverable deliverable)` | Accepts a deliverable (triggers animation) |

**Key Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `transform.position` | `Vector3` | World position of the telepad |

---

### `Immigration`
Singleton managing immigration cycles.

**Key Methods (via Traverse):**
| Method | Description |
|--------|-------------|
| `EndImmigration()` | Ends the current immigration cycle |

**Access:**
```csharp
Immigration.Instance
```

---

## Entity Spawning Patterns

### Spawning Duplicants
```csharp
var personality = Db.Get().Personalities.TryGet(personalityId);
var stats = new MinionStartingStats(personality);
stats.Name = name;
// Apply traits...
GameObject go = stats.Deliver(position);
```

### Spawning Items Directly (Recommended for Client)
```csharp
var prefab = Assets.GetPrefab(new Tag(itemId));
GameObject go = Util.KInstantiate(prefab, position);
go.SetActive(true);
// Set mass if needed:
var pe = go.GetComponent<PrimaryElement>();
if (pe != null) pe.Mass = quantity;
```

### Using Telepad Animation (Host Only)
```csharp
var pkg = new CarePackageInfo(itemId, quantity, null);
GameObject go = pkg.Deliver(position);
```

---

## Database Access

### Personalities
```csharp
Db.Get().Personalities.TryGet("HASSAN") // Returns Personality or null
```

### Traits
```csharp
Db.Get().traits.TryGet("TraitId") // Returns Trait or null
```

### Prefabs
```csharp
Assets.GetPrefab(new Tag("PrefabId")) // Returns GameObject prefab
```

---

## NetworkIdentity Integration

When spawning entities that need to sync:
1. Get or add `NetworkIdentity` component
2. Call `RegisterIdentity()` if NetId is 0
3. Send `EntitySpawnPacket` with the NetId to clients
4. Client creates entity and sets same NetId via `OverrideNetId()`

---

## Building Side Screens

### `ThresholdSwitchSideScreen`
UI for threshold-based sensors (temperature, pressure, hydro, germ, wattage, weight, light sensors).

**Key Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `target` | `IThresholdSwitch` | The threshold switch component |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetTarget(GameObject target)` | Sets the target building |
| `UpdateThresholdValue(float newValue)` | Updates the threshold value |
| `OnConditionButtonClicked()` | Toggles above/below threshold direction |
| `OnMaxValueChanged(float newValue)` | Updates max value |
| `OnMinValueChanged(float newValue)` | Updates min value |

---

### `SingleCheckboxSideScreen`
Generic side screen for buildings with a single checkbox.

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetTarget(GameObject target)` | Sets the target building |

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `target` | `ICheckboxControl` | The control interface |
| `toggle` | `KToggle` | The checkbox UI element |

---

### `CapacityControlSideScreen`
Side screen for buildings with capacity control (storages, reservoirs).

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetTarget(GameObject target)` | Sets the target building |
| `UpdateMaxCapacity(float value)` | Updates the max capacity value |

---

### `TimerSideScreen`
Side screen for timer sensors.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `targetTimedSwitch` | `LogicTimerSensor` | The target timer sensor |
| `onDurationSlider` | `KSlider` | On duration slider |
| `offDurationSlider` | `KSlider` | Off duration slider |
| `cyclesMode` | `bool` | Whether displaying in cycles mode |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetTarget(GameObject target)` | Sets the target building |
| `ChangeSetting()` | Called when slider values change, updates duration values |
| `ToggleMode()` | Toggles between cycles/seconds display mode |
| `ResetTimer()` | Resets the timer |
| `UpdateDurationValueFromTextInput(float newValue, KSlider slider)` | Updates from text input |

---

### `FilterSideScreen`
Side screen for element filter/sensor buildings.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `targetFilterable` | `Filterable` | The target filterable component |
| `isLogicFilter` | `bool` | True for sensors, false for filters |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetTarget(GameObject target)` | Sets the target building |
| `SetFilterTag(Tag tag)` | Sets the selected element/tag |
| `RefreshUI()` | Updates the UI display |

---

### `CounterSideScreen`
Side screen for signal counter buildings.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `targetLogicCounter` | `LogicCounter` | The target counter component |
| `maxCountInput` | `KNumberInputField` | Max count input field |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetTarget(GameObject target)` | Sets the target building |
| `SetMaxCount(int newValue)` | Sets the max count (1-10, wraps) |
| `ToggleAdvanced()` | Toggles advanced mode |
| `ResetCounter()` | Resets counter to 0 |
| `IncrementMaxCount()` | Increments max count |
| `DecrementMaxCount()` | Decrements max count |

---

### `CritterSensorSideScreen`
Side screen for critter count sensors.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `targetSensor` | `LogicCritterCountSensor` | The target sensor |
| `countCrittersToggle` | `KToggle` | Count critters checkbox |
| `countEggsToggle` | `KToggle` | Count eggs checkbox |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetTarget(GameObject target)` | Sets the target building |
| `ToggleCritters()` | Toggles critter counting |
| `ToggleEggs()` | Toggles egg counting |

---

### `IncubatorSideScreen` (extends `ReceptacleSideScreen`)
Side screen for egg incubators.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `continuousToggle` | `MultiToggle` | Auto-replace toggle |

**Access to EggIncubator:**
```csharp
EggIncubator incubator = target.GetComponent<EggIncubator>();
incubator.autoReplaceEntity = true/false;
```

---

### `PlanterSideScreen` (extends `ReceptacleSideScreen`)
Side screen for farm tiles, hydroponic farms, planter boxes.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `selectedSubspecies` | `Tag` | Selected plant subspecies/mutation |
| `mutationPanel` | `GameObject` | Mutation selection UI |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `MutationToggleClicked(GameObject toggle)` | Selects a mutation |
| `RefreshSubspeciesToggles()` | Refreshes mutation options |

---

### `ReceptacleSideScreen` (base class)
Base class for single-entity receptacle buildings.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `targetReceptacle` | `SingleEntityReceptacle` | The target receptacle |
| `selectedDepositObjectTag` | `Tag` | Selected entity tag |
| `selectedDepositObjectAdditionalTag` | `Tag` | Additional filter tag (mutations) |
| `depositObjectMap` | `Dictionary<ReceptacleToggle, SelectableEntity>` | Entity options |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetTarget(GameObject target)` | Sets the target building |
| `ToggleClicked(ReceptacleToggle toggle)` | Handles entity selection |
| `CreateOrder(bool isInfinite)` | Creates the deposit order |
| `UpdateState(object data)` | Updates UI state |

---

## Building Components

### `Door`
Door building component.

**Key Methods:**
| Method | Description |
|--------|-------------|
| `QueueStateChange(ControlState state)` | Queues door state change |
| `OrderOpen()` | Orders door to open |
| `OrderClose()` | Orders door to close |
| `OrderUnseal()` | Orders door to auto mode |

**Enums:**
```csharp
Door.ControlState { Auto, Open, Close }
```

---

### `LimitValve`
Meter/limit valve component.

**Key Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `Limit` | `float` (setter) | Sets the flow limit amount |

---

### `LogicTimerSensor`
Timer sensor component.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `onDuration` | `float` | On duration in seconds |
| `offDuration` | `float` | Off duration in seconds |
| `displayCyclesMode` | `bool` | Display mode |
| `timeElapsedInCurrentState` | `float` | Elapsed time |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `ResetTimer()` | Resets timer elapsed time |
| `OnCopySettings(object data)` | Handles copy/paste |

---

### `LogicTimeOfDaySensor`
Cycle/time-of-day sensor component.

**Key Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `startTime` | `float` (setter) | Start time (0-1 of cycle) |
| `duration` | `float` (setter) | Duration (0-1 of cycle) |

---

### `LogicCounter`
Signal counter component.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `maxCount` | `int` | Maximum count (1-10) |
| `currentCount` | `int` | Current count |
| `advancedMode` | `bool` | Advanced mode flag |
| `resetCountAtMax` | `bool` | Auto-reset at max |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `ResetCounter()` | Resets to 0 |
| `SetCounterState()` | Updates counter state |
| `OnCopySettings(object data)` | Handles copy/paste |

---

### `LogicCritterCountSensor`
Critter count sensor component (implements `IThresholdSwitch`).

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `countCritters` | `bool` | Count critters flag |
| `countEggs` | `bool` | Count eggs flag |
| `countThreshold` | `int` | Threshold count |
| `activateOnGreaterThan` | `bool` | Activation direction |
| `currentCount` | `int` | Current detected count |

---

### `LogicAlarm`
Automated notifier component.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `notificationName` | `string` | Custom notification title |
| `notificationTooltip` | `string` | Custom tooltip text |
| `notificationType` | `NotificationType` | Alert type (enum) |
| `pauseOnNotify` | `bool` | Pause game on trigger |
| `zoomOnNotify` | `bool` | Zoom to building on trigger |
| `cooldown` | `float` | Cooldown between alerts |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `UpdateNotification(bool clear)` | Recreates notification |
| `PushNotification()` | Triggers the notification |
| `OnCopySettings(object data)` | Handles copy/paste |

---

### `TreeFilterable`
Storage filter component (used by storage bins, refrigerators, critter buildings).

**Key Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `AcceptedTags` | `HashSet<Tag>` | Currently accepted tags |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `AddTagToFilter(Tag t)` | Adds a tag to accepted list |
| `RemoveTagFromFilter(Tag t)` | Removes a tag from accepted list |
| `UpdateFilters(HashSet<Tag> filters)` | Bulk update filters |
| `GetTags()` | Returns accepted tags |
| `ContainsTag(Tag t)` | Checks if tag is accepted |
| `OnCopySettings(object data)` | Handles copy/paste |

---

### `Storage`
Core storage component for all buildings with storage.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `capacityKg` | `float` | Max capacity in kg |
| `items` | `List<GameObject>` | Stored items |
| `onlyFetchMarkedItems` | `bool` | "Sweep only" flag |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetOnlyFetchMarkedItems(bool is_set)` | Sets sweep-only mode |
| `Store(GameObject go, ...)` | Stores an item |
| `Drop(GameObject go)` | Drops an item |
| `MassStored()` | Returns total stored mass |
| `RemainingCapacity()` | Returns remaining capacity |

---

### `ComplexFabricator`
Base class for fabricators/cooking stations.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `recipeQueueCounts` | `Dictionary<string, int>` | Recipe ID -> queue count |
| `forbidMutantSeeds` | `bool` | Reject mutant seeds |
| `workingOrderIdx` | `int` | Currently working recipe index |
| `orderProgress` | `float` | Progress 0-1 |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `GetRecipes()` | Returns available recipes |
| `GetRecipe(string id)` | Gets recipe by ID |
| `SetRecipeQueueCount(ComplexRecipe recipe, int count)` | Sets queue count |
| `IncrementRecipeQueueCount(ComplexRecipe recipe)` | Increments queue |
| `DecrementRecipeQueueCount(ComplexRecipe recipe, bool respectInfinite)` | Decrements queue |
| `GetRecipeQueueCount(ComplexRecipe recipe)` | Gets current queue count |
| `OnCopySettings(object data)` | Handles copy/paste |

**Constants:**
```csharp
ComplexFabricator.QUEUE_INFINITE = -1
ComplexFabricator.MAX_QUEUE_SIZE = 99
```

---

### `SingleEntityReceptacle`
Base class for buildings that accept a single entity (planters, incubators).

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `requestedEntityTag` | `Tag` | Requested entity tag |
| `requestedEntityAdditionalFilterTag` | `Tag` | Additional filter (mutations) |
| `Occupant` | `GameObject` | Currently placed entity |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `CreateOrder(Tag entityTag, Tag additionalFilterTag)` | Creates deposit request |
| `CancelActiveRequest()` | Cancels pending request |
| `OrderRemoveOccupant()` | Requests occupant removal |
| `SetPreview(Tag tag)` | Sets preview visual |
| `IsValidEntity(GameObject go)` | Checks if entity is valid |

---

### `EggIncubator` (extends `SingleEntityReceptacle`)
Egg incubator building component.

**Key Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `autoReplaceEntity` | `bool` | Continuous/auto-replace mode |

---

### `BottleEmptier`
Bottle/canister emptier component.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `allowManualPumpingStationFetching` | `bool` | Allow manual pump sources |
| `emit` | `bool` | Currently emitting |
| `emptyRate` | `float` | Emptying rate |
| `isGasEmptier` | `bool` | True for gas, false for liquid |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `OnChangeAllowManualPumpingStationFetching()` | Toggles manual pump setting |
| `OnCopySettings(object data)` | Handles copy/paste |

---

## Interfaces

### `IThresholdSwitch`
Interface for threshold-based switches.

**Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `Threshold` | `float` | Current threshold value |
| `ActivateAboveThreshold` | `bool` | Activation direction |
| `CurrentValue` | `float` | Current measured value |
| `RangeMin` | `float` | Minimum slider value |
| `RangeMax` | `float` | Maximum slider value |

---

### `IUserControlledCapacity`
Interface for buildings with user-controlled capacity.

**Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `UserMaxCapacity` | `float` | User-set max capacity |
| `MinCapacity` | `float` | Minimum allowed |
| `MaxCapacity` | `float` | Maximum allowed |
| `CapacityUnits` | `LocString` | Display units |

---

### `ISliderControl`
Interface for slider-controlled values.

**Methods:**
| Method | Description |
|--------|-------------|
| `GetSliderMin(int index)` | Gets min for slider |
| `GetSliderMax(int index)` | Gets max for slider |
| `GetSliderValue(int index)` | Gets current value |
| `SetSliderValue(float value, int index)` | Sets slider value |

---

### `ICheckboxControl`
Interface for checkbox-controlled values.

**Methods:**
| Method | Description |
|--------|-------------|
| `GetCheckboxValue()` | Gets checkbox state |
| `SetCheckboxValue(bool value)` | Sets checkbox state |

---

## Utility Classes

### `Filterable`
Element filtering component (used by gas/liquid filters and sensors).

**Key Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `SelectedTag` | `Tag` | Currently selected element tag |
| `filterElementState` | `ElementState` | Solid/Liquid/Gas filter type |

---

### `ElementLoader`
Static class for element lookups.

**Methods:**
```csharp
ElementLoader.FindElementByHash(SimHashes hash) // Returns Element
ElementLoader.GetElement(Tag tag) // Returns Element
```

---

### `Tag`
Lightweight identifier for prefabs and categories.

**Constructor:**
```csharp
new Tag(int hash)
new Tag(string name)
```

**Methods:**
```csharp
tag.GetHash() // Returns int hash
tag.IsValid // Returns bool
tag.ProperName() // Returns display name
```

---

### `AccessControl`
Door/building access control component.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `defaultPermissionByTag` | `List<KeyValuePair<Tag, Permission>>` | Default permissions by group tag |
| `savedPermissionsById` | `List<KeyValuePair<int, Permission>>` | Saved per-minion permissions |
| `controlEnabled` | `bool` | Whether access control is enabled |
| `registered` | `bool` | Whether registered in grid |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetDefaultPermission(Tag groupTag, Permission permission)` | Sets default permission for a group (Standard, Bionic) |
| `GetDefaultPermission(Tag groupTag)` | Gets default permission for a group |
| `SetPermission(int id, Permission permission)` | Sets permission for specific minion by ID |
| `ClearPermission(int key, Tag default_key)` | Clears a minion's custom permission |

**Enum `AccessControl.Permission`:**
```csharp
AccessControl.Permission { Both, GoLeft, GoRight, Neither }
```

**Group Tags:**
```csharp
GameTags.Minions.Models.Standard // Regular duplicants
GameTags.Minions.Models.Bionic   // Bionic duplicants
```

---

## Activation Range Buildings

### `IActivationRangeTarget` (Interface)
Interface for buildings with activate/deactivate thresholds (smart batteries, smart reservoirs, massage table).

**Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `ActivateValue` | `float` | Upper threshold value |
| `DeactivateValue` | `float` | Lower threshold value |
| `MinValue` | `float` | Minimum allowed value |
| `MaxValue` | `float` | Maximum allowed value |
| `UseWholeNumbers` | `bool` | Display as integers |
| `ActivateTooltip` | `string` | Tooltip for activate slider |
| `DeactivateTooltip` | `string` | Tooltip for deactivate slider |
| `ActivationRangeTitleText` | `string` | Title text |
| `ActivateSliderLabelText` | `string` | Label for activate slider |
| `DeactivateSliderLabelText` | `string` | Label for deactivate slider |

**Implementations:**
- `BatterySmart` (Smart Battery)
- `SmartReservoir` (Smart Storage Bin, etc.)
- `MassageTable` (Stress relief building)

---

### `ActiveRangeSideScreen`
Side screen for `IActivationRangeTarget` buildings.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `activateValueLabel` | `LocText` | Activate value display |
| `deactivateValueLabel` | `LocText` | Deactivate value display |
| `activateValueSlider` | `KSlider` | Activate threshold slider |
| `deactivateValueSlider` | `KSlider` | Deactivate threshold slider |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetTarget(GameObject target)` | Sets the target building |

---

### `SmartReservoir`
Smart storage component implementing `IActivationRangeTarget`.

**Key Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `ActivateValue` | `float` | Upper threshold (calls UpdateLogicCircuit) |
| `DeactivateValue` | `float` | Lower threshold (calls UpdateLogicCircuit) |

**Internal Fields (via Traverse):**
| Field | Type | Description |
|-------|------|-------------|
| `activateValue` | `float` | Raw activate value field |
| `deactivateValue` | `float` | Raw deactivate value field |

---

## Cycle Sensor

### `TimeRangeSideScreen`
Side screen for `LogicTimeOfDaySensor` (Cycle Sensor).

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `targetTimedSwitch` | `LogicTimeOfDaySensor` | Target sensor component |
| `startTime` | `KSlider` | Start time slider (0-1) |
| `duration` | `KSlider` | Duration slider (0-1) |
| `imageActiveZone` | `Image` | Active zone visualization |
| `imageInactiveZone` | `Image` | Inactive zone visualization |
| `endIndicator` | `RectTransform` | End position indicator |
| `currentTimeMarker` | `RectTransform` | Current time indicator |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetTarget(GameObject target)` | Sets the target building |
| `ChangeSetting()` | Called when sliders change, updates component values |

---

### `LogicTimeOfDaySensor`
Cycle sensor component.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `startTime` | `float` | Start time (0-1 of cycle) |
| `duration` | `float` | Active duration (0-1 of cycle) |

---

## Automated Notifier

### `AlarmSideScreen`
Side screen for `LogicAlarm` (Automated Notifier).

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `targetAlarm` | `LogicAlarm` | Target alarm component |
| `nameInputField` | `KInputField` | Notification name input |
| `tooltipInputField` | `KInputField` | Tooltip text input |
| `pauseToggle` | `KToggle` | Pause on notify checkbox |
| `zoomToggle` | `KToggle` | Zoom on notify checkbox |
| `pauseCheckmark` | `Image` | Pause checkmark visual |
| `zoomCheckmark` | `Image` | Zoom checkmark visual |
| `validTypes` | `List<NotificationType>` | Valid notification types |
| `toggles_by_type` | `Dictionary<NotificationType, MultiToggle>` | Type toggle buttons |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetTarget(GameObject target)` | Sets the target building |
| `OnEndEditName()` | Called when name input finishes |
| `OnEndEditTooltip()` | Called when tooltip input finishes |
| `TogglePause()` | Toggles pause on notify |
| `ToggleZoom()` | Toggles zoom on notify |
| `SelectType(NotificationType type)` | Selects notification type |
| `UpdateVisuals()` | Updates UI from component values |
| `RefreshToggles()` | Refreshes type toggle buttons |
| `UpdateNotification(bool clear)` | Triggers notification update |

---

## GeoTuner (Geyser Tuner)

### `GeoTuner` (StateMachine)
StateMachine for geyser tuning buildings.

**Key Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `FutureGeyser` | `TargetParameter` | Queued geyser to tune |
| `AssignedGeyser` | `TargetParameter` | Currently assigned geyser |
| `hasBeenWorkedByResearcher` | `BoolParameter` | Research completed flag |
| `expirationTimer` | `FloatParameter` | Tuning duration remaining |

---

### `GeoTuner.Instance`
StateMachine instance for GeoTuner.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `operational` | `Operational` | Operational status |
| `storage` | `Storage` | Material storage |
| `workable` | `GeoTunerWorkable` | Research workable |
| `switchGeyserChore` | `Chore` | Geyser switching chore |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `GetFutureGeyser()` | Returns queued geyser (or null) |
| `GetAssignedGeyser()` | Returns currently assigned geyser (or null) |
| `AssignFutureGeyser(Geyser geyser)` | Queues a geyser for tuning (or null to clear) |
| `AssignGeyser(Geyser geyser)` | Directly assigns a geyser |
| `OnCopySettings(object data)` | Handles copy/paste |

**Access Pattern:**
```csharp
var geoTuner = gameObject.GetSMI<GeoTuner.Instance>();
geoTuner.AssignFutureGeyser(geyser); // or null to clear
```

---

### `GeoTunerSideScreen`
Side screen for GeoTuner buildings.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `targetGeotuner` | `GeoTuner.Instance` | Target GeoTuner instance |
| `rowPrefab` | `GameObject` | Geyser row prefab |
| `rowContainer` | `RectTransform` | Container for rows |
| `rows` | `Dictionary<object, GameObject>` | Geyser row GameObjects |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetTarget(GameObject target)` | Sets the target building |
| `RefreshOptions(object data)` | Rebuilds the geyser list |
| `SetRow(int idx, string name, Sprite icon, Geyser geyser, bool studied)` | Sets up a geyser row |
| `ClearRows()` | Removes all rows |

---

### `Components.Geysers`
Global collection of all geysers.

**Access:**
```csharp
global::Components.Geysers.GetItems(worldId) // Returns List<Geyser>
```

**Note:** Use `global::` prefix to avoid namespace conflict with mod's `Components` namespace.

---

## Missile Launcher (Meteor Blaster)

### `IMissileSelectionInterface` (Interface)
Interface for missile selection.

**Methods:**
| Method | Return | Description |
|--------|--------|-------------|
| `AmmunitionIsAllowed(Tag tag)` | `bool` | Checks if ammo type is allowed |
| `IsAnyCosmicBlastShotAllowed()` | `bool` | Any cosmic blast shot allowed |
| `ChangeAmmunition(Tag tag, bool allowed)` | `void` | Sets ammo type allowed state |
| `OnRowToggleClick()` | `void` | Called after toggle click |
| `GetValidAmmunitionTags()` | `List<Tag>` | Gets valid ammo types |

---

### `MissileLauncher` (StateMachine)
StateMachine for missile launcher buildings.

**Key States:**
- `Off` - Not operational
- `On.searching` - Scanning for meteors
- `On.idle` - Idle (no meteors detected)
- `Launch.targeting` - Aiming at meteor
- `Launch.shoot` - Firing missile
- `Cooldown` - Post-fire cooldown

---

### `MissileLauncher.Instance`
StateMachine instance implementing `IMissileSelectionInterface`.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `ammunitionPermissions` | `Dictionary<Tag, bool>` | Ammo type -> allowed |
| `MissileStorage` | `Storage` | Basic missile storage |
| `LongRangeStorage` | `Storage` | Long-range missile storage |
| `clusterDestinationSelector` | `EntityClusterDestinationSelector` | Target selector |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `ChangeAmmunition(Tag tag, bool allowed)` | Enables/disables ammo type |
| `AmmunitionIsAllowed(Tag tag)` | Checks if ammo type is allowed |
| `UpdateAmmunitionDelivery()` | Updates delivery requests |
| `LaunchMissile()` | Fires a basic missile |
| `LaunchLongRangeMissile()` | Fires a long-range missile |
| `OnCopySettings(object data)` | Handles copy/paste |

**Access Pattern:**
```csharp
var launcher = gameObject.GetSMI<MissileLauncher.Instance>();
launcher.ChangeAmmunition(new Tag("MissileBasic"), true);
```

---

### `MissileSelectionSideScreen`
Side screen for missile selection.

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `targetMissileLauncher` | `IMissileSelectionInterface` | Target launcher |
| `rowPrefab` | `GameObject` | Ammo row prefab |
| `listContainer` | `GameObject` | Container for rows |
| `ammunitiontags` | `List<Tag>` | Valid ammunition types |
| `rows` | `Dictionary<Tag, GameObject>` | Tag -> row mapping |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetTarget(GameObject target)` | Sets the target building |
| `Build()` | Rebuilds the ammo list |
| `Refresh()` | Updates toggle states |

---

## Crafting / Fabrication

### `ComplexFabricatorSideScreen`
Side screen for fabricators (cooking stations, refineries).

**Key Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `targetFab` | `ComplexFabricator` | Target fabricator |
| `recipeGrid` | `GameObject` | Recipe button container |
| `recipeCategoryToggleMap` | `Dictionary<GameObject, List<ComplexRecipe>>` | Toggle -> recipes |
| `recipeToggles` | `List<GameObject>` | Recipe toggle GameObjects |
| `selectedRecipeCategory` | `string` | Currently selected category |
| `selectedToggle` | `KToggle` | Currently selected toggle |
| `recipeScreen` | `SelectedRecipeQueueScreen` | Secondary queue screen |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `SetTarget(GameObject target)` | Sets the target building |
| `Initialize(ComplexFabricator target)` | Initializes recipe list |
| `ToggleClicked(KToggle toggle)` | Handles recipe category selection |
| `RefreshQueueCountDisplay(GameObject entryGO, ComplexFabricator fabricator)` | Updates queue display |
| `RefreshQueueCountDisplayForRecipeCategory(string categoryID, ComplexFabricator fabricator)` | Updates specific category |
| `UpdateQueueCountLabels(object data)` | Updates all queue labels |
| `CycleRecipe(int increment)` | Cycles through recipes |

**Style Settings:**
```csharp
ComplexFabricatorSideScreen.StyleSetting {
    GridResult, ListResult, GridInput, ListInput,
    ListInputOutput, GridInputOutput, ClassicFabricator, ListQueueHybrid
}
```

---

### `ComplexRecipe`
Represents a fabrication recipe.

**Key Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `id` | `string` | Unique recipe ID |
| `recipeCategoryID` | `string` | Category grouping ID |
| `ingredients` | `RecipeElement[]` | Required inputs |
| `results` | `RecipeElement[]` | Produced outputs |
| `description` | `string` | Recipe description |
| `requiredTech` | `string` | Required tech ID (or null) |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `GetUIName(bool includeAmounts)` | Gets display name |
| `IsRequiredTechUnlocked()` | Checks if tech is researched |
| `RequiresTechUnlock()` | Whether tech is required |
