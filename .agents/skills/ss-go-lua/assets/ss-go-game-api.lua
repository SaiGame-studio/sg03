---@meta ss-go-game-api

-- Editor-only contract stub for ss-go studio Lua scripts.
-- This file documents globals injected by the server runtime. It is not executed by production code.

---@alias UUID string
---@alias LuaError string|nil
---@alias LuaMap table<string, any>
---@alias LuaList any[]
---@alias ISO8601Timestamp string

---@class SSItemTag
---@field id UUID
---@field game_id UUID
---@field tag_key string
---@field label string
---@field color string
---@field metadata LuaMap
---@field created_by? UUID
---@field created_at ISO8601Timestamp
---@field updated_at ISO8601Timestamp
---@field item_count integer

---@class SSItemDefinition
---@field id UUID
---@field game_id UUID
---@field item_code string
---@field name string
---@field category string
---@field rarity string
---@field base_stats LuaMap
---@field metadata LuaMap
---@field is_stackable boolean
---@field max_stack_size integer|nil
---@field grid_width integer
---@field grid_height integer
---@field client_writable boolean
---@field allow_client_update_qty boolean
---@field deleted_at? ISO8601Timestamp
---@field created_by? UUID
---@field updated_by? UUID
---@field created_at ISO8601Timestamp
---@field updated_at ISO8601Timestamp
---@field tags? SSItemTag[]

---@class SSInventoryItem
---@field id UUID
---@field game_id UUID
---@field user_id UUID
---@field item_definition_id UUID
---@field item_container_id UUID
---@field grid_x integer
---@field grid_y integer
---@field quantity integer
---@field level integer
---@field private_properties LuaMap
---@field public_properties LuaMap
---@field acquired_at ISO8601Timestamp
---@field last_modified_at ISO8601Timestamp
---@field deleted_at? ISO8601Timestamp
---@field created_by? UUID
---@field updated_by? UUID
---@field version integer
---@field equipped_slot_key? string
---@field slot_data? LuaMap
---@field definition? SSItemDefinition
---@field container? SSItemContainer

---@class SSItemContainerDefinition
---@field id UUID
---@field game_id UUID
---@field code_name string
---@field name string
---@field container_type string
---@field grid_cols integer
---@field grid_rows integer
---@field is_portable boolean
---@field instanced_per_item boolean
---@field linked_item_definition_id? UUID
---@field metadata LuaMap
---@field created_by? UUID
---@field updated_by? UUID
---@field deleted_at? ISO8601Timestamp
---@field created_at ISO8601Timestamp
---@field updated_at ISO8601Timestamp

---@class SSItemContainer
---@field id UUID
---@field game_id UUID
---@field owner_user_id UUID
---@field item_container_definition_id UUID
---@field source_item_instance_id? UUID
---@field container_type string
---@field name? string
---@field inventory_item_id? UUID
---@field position_data LuaMap
---@field metadata LuaMap
---@field created_by? UUID
---@field deleted_at? ISO8601Timestamp
---@field created_at ISO8601Timestamp
---@field updated_at ISO8601Timestamp
---@field definition? SSItemContainerDefinition

---@class SSGachaDropEntry
---@field item_definition_id UUID
---@field weight integer
---@field quantity_min integer
---@field quantity_max integer

---@class SSGachaKeyRequirement
---@field item_definition_id UUID
---@field quantity integer

---@class SSGachaPack
---@field id UUID
---@field game_id UUID
---@field code_name string
---@field name string
---@field item_pool SSGachaDropEntry[]
---@field collect_destination "mailbox"|"inventory"
---@field key_requirements SSGachaKeyRequirement[]
---@field metadata? LuaMap
---@field is_enabled boolean
---@field created_at ISO8601Timestamp
---@field updated_at ISO8601Timestamp

---@class SSGachaGrantedItem
---@field item_definition_id UUID
---@field name string
---@field quantity integer
---@field rarity string
---@field category string

---@class SSGachaOpenResult
---@field transaction_id UUID
---@field collect_destination string
---@field mailbox_message_id UUID
---@field is_duplicate boolean
---@field items SSGachaGrantedItem[]

---@class SSQuestItemRequirement
---@field item_definition_id UUID
---@field quantity integer

---@class SSQuestGachaPackRequirement
---@field gacha_pack_id UUID
---@field quantity integer

---@class SSQuestConditionClause
---@field clause_id? string
---@field type? string
---@field target? integer
---@field details? LuaMap
---@field items? SSQuestItemRequirement[]
---@field packs? SSQuestGachaPackRequirement

---@class SSQuestConditionGroup
---@field operator "AND"|"OR"
---@field clauses SSQuestConditionClause[]

---@class SSQuestReward
---@field reward_type string
---@field item_definition_id? UUID
---@field quantity_min? integer
---@field quantity_max? integer
---@field amount? integer

---@class SSQuestDefinition
---@field id UUID
---@field game_id UUID
---@field code_name string
---@field name string
---@field description string
---@field quest_type "one_time"|"daily"
---@field conditions SSQuestConditionGroup
---@field rewards SSQuestReward[]
---@field is_active boolean
---@field expire_after_minutes? integer
---@field metadata? LuaMap
---@field deleted_at? ISO8601Timestamp
---@field created_at ISO8601Timestamp
---@field updated_at ISO8601Timestamp

---@class SSGameEventType
---@field id UUID
---@field game_id UUID
---@field event_type string
---@field description string
---@field created_by UUID
---@field created_at ISO8601Timestamp

---@class SSEntityDefinition
---@field id UUID
---@field game_id UUID
---@field entity_key string
---@field entity_type string
---@field name string
---@field description? string
---@field icon_url? string
---@field rarity? string
---@field stats LuaMap
---@field abilities LuaMap[]
---@field metadata LuaMap
---@field is_active boolean
---@field deleted_at? ISO8601Timestamp
---@field created_at ISO8601Timestamp
---@field updated_at ISO8601Timestamp

---@class SSEntityPoolSelection
---@field id UUID
---@field entity_key string
---@field entity_type string
---@field name string
---@field stats LuaMap
---@field rarity? string

---@class SSEntityPoolEntry
---@field id UUID
---@field pool_id UUID
---@field entity_definition_id UUID
---@field weight integer
---@field created_at ISO8601Timestamp
---@field updated_at ISO8601Timestamp
---@field entity_key string
---@field entity_type string
---@field entity_name string
---@field rarity? string
---@field stats LuaMap

---@class SSEntityPool
---@field id UUID
---@field game_id UUID
---@field pool_key string
---@field name string
---@field description? string
---@field metadata? LuaMap
---@field is_active boolean
---@field created_at ISO8601Timestamp
---@field updated_at ISO8601Timestamp
---@field entries SSEntityPoolEntry[]

---@class SSPresetDefinition
---@field id UUID
---@field game_id UUID
---@field code_name string
---@field preset_type string
---@field name string
---@field max_slots integer
---@field metadata LuaMap
---@field created_at ISO8601Timestamp
---@field updated_at ISO8601Timestamp
---@field deleted_at? ISO8601Timestamp

---@class SSPreset
---@field id UUID
---@field game_id UUID
---@field user_id UUID
---@field definition_id? UUID
---@field preset_type string
---@field name? string
---@field max_slots integer
---@field metadata LuaMap
---@field created_at ISO8601Timestamp
---@field updated_at ISO8601Timestamp
---@field deleted_at? ISO8601Timestamp

---@class SSPresetSlot
---@field id UUID
---@field container_id UUID
---@field slot_index integer
---@field inventory_item_id UUID
---@field item_definition_id UUID
---@field item_definition_name string
---@field item_definition_code_name string
---@field created_at ISO8601Timestamp

---@class SSEntityDropPackResult
---@field pack_id UUID
---@field pack_name string
---@field success boolean
---@field items SSGachaGrantedItem[]
---@field error? string

---@class SSGameDetail
---@field id UUID
---@field studio_id UUID
---@field name string
---@field description string|nil
---@field tags string[]
---@field status "development"|"alpha"|"beta"|"released"|"archived"
---@field is_active boolean
---@field limits LuaMap   -- effective resource limits (system defaults + plugin boosts)
---@field usage LuaMap    -- current usage counters per resource key
---@field settings LuaMap -- studio-configurable game-level options (JSONB)
---@field created_at number
---@field updated_at number

---@class SSContext
---@field player_id UUID
---@field game_id UUID
---@field studio_id UUID
---@field timestamp number
---@field script_version number Integer version of the currently executing script definition.
---@field game SSGameDetail Full detail of the current game (always present when server wires gameRepo).
---@field [string] any

---@class SSGameAPI
local SSGameAPI = {}

---Append a message to the execution log. Max 100 lines.
---@param msg any
function SSGameAPI.log(msg) end

---Grant item units to the authenticated player.
---@param item_def_id UUID
---@param amount integer
---@return LuaError err
function SSGameAPI.grant_item(item_def_id, amount) end

---Deduct item units from the authenticated player.
---@param item_def_id UUID
---@param amount integer
---@return LuaError err
function SSGameAPI.deduct_item(item_def_id, amount) end

---Fetch an item definition by UUID.
---@param id UUID
---@return SSItemDefinition|nil item_def
---@return LuaError err
function SSGameAPI.get_item_def_by_id(id) end

---Fetch an item definition by code.
---@param code string
---@return SSItemDefinition|nil item_def
---@return LuaError err
function SSGameAPI.get_item_def_by_code(code) end

---Fetch multiple item definitions by UUID array in a single DB query.
---@param ids UUID[]
---@return SSItemDefinition[]|nil item_defs
---@return LuaError err
function SSGameAPI.get_item_defs_by_ids(ids) end

---Fetch multiple item definitions by code array in a single DB query.
---@param codes string[]
---@return SSItemDefinition[]|nil item_defs
---@return LuaError err
function SSGameAPI.get_item_defs_by_codes(codes) end

---Fetch a player inventory item instance by UUID.
---@param id UUID
---@return SSInventoryItem|nil item
---@return LuaError err
function SSGameAPI.get_item_instance_by_id(id) end

---Merge private properties into an inventory item. The level key is reserved.
---@param item_id UUID
---@param version integer
---@param props LuaMap
---@return LuaError err
function SSGameAPI.update_item_private_properties(item_id, version, props) end

---Fetch an item container definition by UUID.
---@param id UUID
---@return SSItemContainerDefinition|nil container_def
---@return LuaError err
function SSGameAPI.get_container_def_by_id(id) end

---Fetch a player item container by UUID.
---@param id UUID
---@return SSItemContainer|nil container
---@return LuaError err
function SSGameAPI.get_container_by_id(id) end

---Fetch a gacha pack definition by UUID.
---@param id UUID
---@return SSGachaPack|nil pack
---@return LuaError err
function SSGameAPI.get_gacha_pack_by_id(id) end

---Open one gacha pack for the authenticated player.
---@param pack_id UUID
---@param container_id? UUID
---@param idempotency_key? string
---@return SSGachaOpenResult|nil result
---@return LuaError err
function SSGameAPI.open_gacha_pack(pack_id, container_id, idempotency_key) end

---Fetch a quest definition by UUID.
---@param id UUID
---@return SSQuestDefinition|nil quest_def
---@return LuaError err
function SSGameAPI.get_quest_def_by_id(id) end

---Fetch an event type by UUID or name.
---@param id_or_name string
---@return SSGameEventType|nil event_type
---@return LuaError err
function SSGameAPI.get_event_type_by_id(id_or_name) end

---Fetch an event type by name.
---@param name string
---@return SSGameEventType|nil event_type
---@return LuaError err
function SSGameAPI.get_event_type_by_name(name) end

---Fetch an entity definition by UUID.
---@param id UUID
---@return SSEntityDefinition|nil entity_def
---@return LuaError err
function SSGameAPI.get_entity_def_by_id(id) end

---Fetch an entity definition by key.
---@param key string
---@return SSEntityDefinition|nil entity_def
---@return LuaError err
function SSGameAPI.get_entity_def_by_key(key) end

---Pick one weighted random entity from an entity pool.
---@param pool_key string
---@return SSEntityPoolSelection|nil entity
---@return LuaError err
function SSGameAPI.entity_pool_random(pool_key) end

---Get entities with the lowest stat value in a pool. Always returns a list. Count defaults to 1 and is capped at 100.
---@param pool_key string
---@param stat_key string
---@param count? integer
---@return SSEntityPoolSelection[]|nil result
---@return LuaError err
function SSGameAPI.entity_pool_min(pool_key, stat_key, count) end

---Get entities with the highest stat value in a pool. Always returns a list. Count defaults to 1 and is capped at 100.
---@param pool_key string
---@param stat_key string
---@param count? integer
---@return SSEntityPoolSelection[]|nil result
---@return LuaError err
function SSGameAPI.entity_pool_max(pool_key, stat_key, count) end

---Fetch an entity pool definition by UUID.
---@param id UUID
---@return SSEntityPool|nil pool_def
---@return LuaError err
function SSGameAPI.get_entity_pool_def_by_id(id) end

---Fetch an entity pool definition by key.
---@param pool_key string
---@return SSEntityPool|nil pool_def
---@return LuaError err
function SSGameAPI.get_entity_pool_def_by_key(pool_key) end

---Fetch a preset definition by UUID.
---@param id UUID
---@return SSPresetDefinition|nil preset_def
---@return LuaError err
function SSGameAPI.get_preset_def_by_id(id) end

---Fetch a preset instance by UUID.
---@param id UUID
---@return SSPreset|nil preset
---@return LuaError err
function SSGameAPI.get_preset_by_id(id) end

---Fetch all slots for a preset instance.
---@param preset_id UUID
---@return SSPresetSlot[]|nil slots
---@return LuaError err
function SSGameAPI.get_preset_slots(preset_id) end

---Fetch the authenticated player's equipped item in a slot.
---@param slot_key string
---@return SSInventoryItem|nil item
---@return LuaError err
function SSGameAPI.get_equipped_in_slot(slot_key) end

---Create an active battle session.
---@param state LuaMap
---@return UUID|nil session_id
---@return LuaError err
function SSGameAPI.battle_session_create(state) end

---Return the current active battle session ID for the script's game/player context.
---@return UUID|nil session_id
---@return LuaError err
function SSGameAPI.battle_session_current_id() end

---Read battle session state.
---@param session_id UUID
---@return LuaMap|nil state
---@return LuaError err
function SSGameAPI.battle_session_get(session_id) end

---Overwrite battle session state.
---@param session_id UUID
---@param state LuaMap
---@return LuaError err
function SSGameAPI.battle_session_update(session_id, state) end

---End a battle session.
---@param session_id UUID
---@param end_data? LuaMap
---@return LuaError err
function SSGameAPI.battle_session_end(session_id, end_data) end

---Mark a battle session as fled.
---@param session_id UUID
---@return LuaError err
function SSGameAPI.battle_session_flee(session_id) end

---Open all configured entity drop packs for a defeated entity. Max 7 pack IDs.
---@param session_id UUID
---@param entity_def_id UUID
---@param pack_ids UUID[]
---@return SSEntityDropPackResult[]|nil results
---@return LuaError err
function SSGameAPI.open_entity_drop_packs(session_id, entity_def_id, pack_ids) end

---@type LuaMap
payload = payload

---@type SSContext
ctx = ctx

---@type LuaMap
output = output

---@type SSGameAPI
game = game

---Alias for game.log.
---@param msg any
function print(msg) end

return SSGameAPI
