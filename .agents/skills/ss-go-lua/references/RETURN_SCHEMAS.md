# `game.*` Return Schemas

This reference defines the Lua table fields returned by the ss-go runtime. Field names mirror the backend JSON keys. A field marked optional may be absent. UUIDs and timestamps are strings; timestamps use RFC 3339 encoding. `map` means a Lua table with string keys and game-defined values.

## Contents

- [Function mapping](#function-mapping)
- [Inventory schemas](#inventory-schemas)
- [Gacha schemas](#gacha-schemas)
- [Quest schema](#quest-schema)
- [Event and entity schemas](#event-and-entity-schemas)
- [Preset schemas](#preset-schemas)
- [Battle schemas](#battle-schemas)

## Function mapping

| Function | Success value |
|---|---|
| `game.get_item_def_by_id`, `game.get_item_def_by_code` | `SSItemDefinition` |
| `game.get_item_defs_by_ids`, `game.get_item_defs_by_codes` | `SSItemDefinition[]` |
| `game.get_item_instance_by_id`, `game.get_equipped_in_slot` | `SSInventoryItem` |
| `game.get_container_def_by_id` | `SSItemContainerDefinition` |
| `game.get_container_by_id` | `SSItemContainer` |
| `game.get_gacha_pack_by_id` | `SSGachaPack` |
| `game.open_gacha_pack` | `SSGachaOpenResult` |
| `game.get_quest_def_by_id` | `SSQuestDefinition` |
| `game.get_event_type_by_id`, `game.get_event_type_by_name` | `SSGameEventType` |
| `game.get_entity_def_by_id`, `game.get_entity_def_by_key` | `SSEntityDefinition` |
| `game.entity_pool_random` | `SSEntityPoolSelection` |
| `game.entity_pool_min`, `game.entity_pool_max` | `SSEntityPoolSelection[]` |
| `game.get_entity_pool_def_by_id`, `game.get_entity_pool_def_by_key` | `SSEntityPool` |
| `game.get_preset_def_by_id` | `SSPresetDefinition` |
| `game.get_preset_by_id` | `SSPreset` |
| `game.get_preset_slots` | `SSPresetSlot[]` |
| `game.battle_session_create`, `game.battle_session_current_id` | UUID string |
| `game.battle_session_get` | Game-defined state table previously supplied by the game |
| `game.open_entity_drop_packs` | `SSEntityDropPackResult[]` |

All lookup functions also return `err` as their second result. Do not access the success value until `err` is nil. `entity_pool_min` and `entity_pool_max` always return a list, including when the requested or default count is one.

## Inventory schemas

### `SSItemDefinition`

| Field | Type | Notes |
|---|---|---|
| `id` | UUID | |
| `game_id` | UUID | |
| `item_code` | string | |
| `name` | string | |
| `category` | string | |
| `rarity` | string | |
| `base_stats` | map | |
| `metadata` | map | |
| `is_stackable` | boolean | |
| `max_stack_size` | integer or nil | Reads as nil when the item is not stackable. |
| `grid_width`, `grid_height` | integer | |
| `client_writable` | boolean | |
| `allow_client_update_qty` | boolean | |
| `deleted_at` | timestamp | Optional. |
| `created_by`, `updated_by` | UUID | Optional. |
| `created_at`, `updated_at` | timestamp | |
| `tags` | `SSItemTag[]` | Optional. |

### `SSItemTag`

| Field | Type |
|---|---|
| `id`, `game_id` | UUID |
| `tag_key`, `label`, `color` | string |
| `metadata` | map |
| `created_by` | UUID, optional |
| `created_at`, `updated_at` | timestamp |
| `item_count` | integer |

### `SSInventoryItem`

| Field | Type |
|---|---|
| `id`, `game_id`, `user_id`, `item_definition_id`, `item_container_id` | UUID |
| `grid_x`, `grid_y`, `quantity`, `level`, `version` | integer |
| `private_properties`, `public_properties` | map |
| `acquired_at`, `last_modified_at` | timestamp |
| `deleted_at` | timestamp, optional |
| `created_by`, `updated_by` | UUID, optional |
| `equipped_slot_key` | string, optional |
| `slot_data` | map, optional |
| `definition` | `SSItemDefinition`, optional |
| `container` | `SSItemContainer`, optional |

### `SSItemContainerDefinition`

| Field | Type |
|---|---|
| `id`, `game_id` | UUID |
| `code_name`, `name`, `container_type` | string |
| `grid_cols`, `grid_rows` | integer |
| `is_portable`, `instanced_per_item` | boolean |
| `linked_item_definition_id` | UUID, optional |
| `metadata` | map |
| `created_by`, `updated_by` | UUID, optional |
| `deleted_at` | timestamp, optional |
| `created_at`, `updated_at` | timestamp |

### `SSItemContainer`

| Field | Type |
|---|---|
| `id`, `game_id`, `owner_user_id`, `item_container_definition_id` | UUID |
| `source_item_instance_id`, `inventory_item_id`, `created_by` | UUID, optional |
| `container_type` | string |
| `name` | string, optional |
| `position_data`, `metadata` | map |
| `deleted_at` | timestamp, optional |
| `created_at`, `updated_at` | timestamp |
| `definition` | `SSItemContainerDefinition`, optional |

## Gacha schemas

### `SSGachaPack`

| Field | Type |
|---|---|
| `id`, `game_id` | UUID |
| `code_name`, `name` | string |
| `item_pool` | `SSGachaDropEntry[]` |
| `collect_destination` | `"mailbox"` or `"inventory"` |
| `key_requirements` | `SSGachaKeyRequirement[]` |
| `metadata` | map, optional |
| `is_enabled` | boolean |
| `created_at`, `updated_at` | timestamp |

`SSGachaDropEntry` has `item_definition_id` (UUID), `weight` (integer), `quantity_min` (integer), and `quantity_max` (integer).

`SSGachaKeyRequirement` has `item_definition_id` (UUID) and `quantity` (integer).

### `SSGachaOpenResult`

| Field | Type |
|---|---|
| `transaction_id` | UUID |
| `collect_destination` | string |
| `mailbox_message_id` | UUID |
| `is_duplicate` | boolean |
| `items` | `SSGachaGrantedItem[]` |

`SSGachaGrantedItem` has `item_definition_id` (UUID), `name` (string), `quantity` (integer), `rarity` (string), and `category` (string).

## Quest schema

### `SSQuestDefinition`

| Field | Type |
|---|---|
| `id`, `game_id` | UUID |
| `code_name`, `name`, `description` | string |
| `quest_type` | `"one_time"` or `"daily"` |
| `conditions` | `SSQuestConditionGroup` |
| `rewards` | `SSQuestReward[]` |
| `is_active` | boolean |
| `expire_after_minutes` | integer, optional |
| `metadata` | map, optional |
| `deleted_at` | timestamp, optional |
| `created_at`, `updated_at` | timestamp |

`SSQuestConditionGroup` has `operator` (`"AND"` or `"OR"`) and `clauses` (`SSQuestConditionClause[]`).

`SSQuestConditionClause` may contain `clause_id` (string), `type` (string), `target` (integer), `details` (map), `items` (`SSQuestItemRequirement[]`), and `packs` (`SSQuestGachaPackRequirement`). Every field is optional. An item requirement has `item_definition_id` (UUID) and `quantity` (integer). A pack requirement has `gacha_pack_id` (UUID) and `quantity` (integer).

`SSQuestReward` always has `reward_type` (string). It may have `item_definition_id` (UUID), `quantity_min` (integer), `quantity_max` (integer), or `amount` (integer).

## Event and entity schemas

### `SSGameEventType`

| Field | Type |
|---|---|
| `id`, `game_id`, `created_by` | UUID |
| `event_type`, `description` | string |
| `created_at` | timestamp |

### `SSEntityDefinition`

| Field | Type |
|---|---|
| `id`, `game_id` | UUID |
| `entity_key`, `entity_type`, `name` | string |
| `description`, `icon_url`, `rarity` | string, optional |
| `stats`, `metadata` | map |
| `abilities` | map[] | Each ability requires an `id`; other keys are game-defined. |
| `is_active` | boolean |
| `deleted_at` | timestamp, optional |
| `created_at`, `updated_at` | timestamp |

### `SSEntityPoolSelection`

| Field | Type |
|---|---|
| `id` | UUID | Entity definition ID. |
| `entity_key`, `entity_type`, `name` | string | |
| `stats` | map | |
| `rarity` | string, optional | |

### `SSEntityPool`

| Field | Type |
|---|---|
| `id`, `game_id` | UUID |
| `pool_key`, `name` | string |
| `description` | string, optional |
| `metadata` | map, optional |
| `is_active` | boolean |
| `created_at`, `updated_at` | timestamp |
| `entries` | `SSEntityPoolEntry[]` |

`SSEntityPoolEntry` has `id`, `pool_id`, and `entity_definition_id` (UUID); `weight` (integer); `created_at` and `updated_at` (timestamp); `entity_key`, `entity_type`, and `entity_name` (string); `rarity` (optional string); and `stats` (map).

## Preset schemas

### `SSPresetDefinition`

| Field | Type |
|---|---|
| `id`, `game_id` | UUID |
| `code_name`, `preset_type`, `name` | string |
| `max_slots` | integer |
| `metadata` | map |
| `created_at`, `updated_at` | timestamp |
| `deleted_at` | timestamp, optional |

### `SSPreset`

| Field | Type |
|---|---|
| `id`, `game_id`, `user_id` | UUID |
| `definition_id` | UUID, optional |
| `preset_type` | string |
| `name` | string, optional |
| `max_slots` | integer |
| `metadata` | map |
| `created_at`, `updated_at` | timestamp |
| `deleted_at` | timestamp, optional |

### `SSPresetSlot`

| Field | Type |
|---|---|
| `id`, `container_id`, `inventory_item_id`, `item_definition_id` | UUID |
| `slot_index` | integer |
| `item_definition_name`, `item_definition_code_name` | string |
| `created_at` | timestamp |

## Battle schemas

Battle session state and end data are game-defined maps; the runtime returns the same state shape supplied by the game.

`SSEntityDropPackResult` has `pack_id` (UUID), `pack_name` (string), and `success` (boolean). It has `error` (string) only when that pack failed. After argument validation succeeds, individual pack failures are reported in these entries rather than as the function-level `err`; inspect each entry's `success` and optional `error`.
