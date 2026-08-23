-- lib_ability_xena
-- is_library = true

-- Shared execution for Xena's awakened Ability cards.
-- config requires ability_key, successor_code, and successor_name.
-- sacrifice_count supports 0 (default), 1, or 2 adjacent allied cards.
-- sacrifice_stars is an optional list of allowed card stars (each 1 through 9).
-- sacrifice_race restricts sacrifices to one card race when provided.
-- sacrifice_excluded_codes is an optional list of card definition codes that cannot be sacrificed.

local function validate_xena_config(config)
    if config == nil or type(config.ability_key) ~= "string" or
       type(config.successor_code) ~= "string" or type(config.successor_name) ~= "string" then
        return nil, "xena_awakened requires valid configuration"
    end

    local sacrifice_count = config.sacrifice_count or 0
    if type(sacrifice_count) ~= "number" or sacrifice_count ~= math.floor(sacrifice_count) or
       sacrifice_count < 0 or sacrifice_count > 2 then
        return nil, config.ability_key .. " sacrifice_count must be 0, 1, or 2"
    end

    local sacrifice_star_set = nil
    if config.sacrifice_stars ~= nil then
        if type(config.sacrifice_stars) ~= "table" then
            return nil, config.ability_key .. " sacrifice_stars must be a list"
        end
        sacrifice_star_set = {}
        for _, star in ipairs(config.sacrifice_stars) do
            if type(star) ~= "number" or star ~= math.floor(star) or star < 1 or star > 9 then
                return nil, config.ability_key .. " sacrifice_stars entries must be integers from 1 to 9"
            end
            sacrifice_star_set[star] = true
        end
    end

    local sacrifice_race = config.sacrifice_race
    if sacrifice_race ~= nil and type(sacrifice_race) ~= "string" then
        return nil, config.ability_key .. " sacrifice_race must be a string"
    end

    local sacrifice_excluded_code_set = {}
    if config.sacrifice_excluded_codes ~= nil then
        if type(config.sacrifice_excluded_codes) ~= "table" then
            return nil, config.ability_key .. " sacrifice_excluded_codes must be a list"
        end
        for _, code in ipairs(config.sacrifice_excluded_codes) do
            if type(code) ~= "string" then
                return nil, config.ability_key .. " sacrifice_excluded_codes entries must be strings"
            end
            sacrifice_excluded_code_set[code] = true
        end
    end

    return {
        ability_key = config.ability_key,
        successor_code = config.successor_code,
        successor_name = config.successor_name,
        sacrifice_count = sacrifice_count,
        sacrifice_star_set = sacrifice_star_set,
        sacrifice_race = sacrifice_race,
        sacrifice_excluded_code_set = sacrifice_excluded_code_set,
    }, nil
end

local function find_xena_target(state, source_card, event_data, helpers, ability_key)
    local target_card = (event_data or {}).defender_card
    if target_card == nil then return nil, ability_key .. " requires a target card" end

    local target_def = helpers.find_item_def(state.item_defs, target_card.item_definition_code_name)
    local target_type = target_def ~= nil and target_def.metadata ~= nil and target_def.metadata.type or nil
    if target_type ~= "character" then return nil, ability_key .. " target must be a Character" end
    if not helpers.is_character_be_attacked(state, target_card) then
        return nil, ability_key .. " target is not being attacked"
    end

    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return nil, ability_key .. " source card is not on a battle line"
    end
    return { target_card = target_card, source_side = source_side }, nil
end

local function find_target_line(state, event_data, target_card, ability_key)
    local target_line_key = (event_data or {}).defender_line_key
    local target_line = target_line_key ~= nil and state[target_line_key] or nil
    if target_line ~= nil then
        for index, card in ipairs(target_line) do
            if card.inventory_item_id == target_card.inventory_item_id then
                return target_line_key, target_line, index, nil
            end
        end
    end
    return nil, nil, nil, ability_key .. " target must be on a battle line"
end

local function find_successor_card(void_zone, successor_code)
    for index, card in ipairs(void_zone) do
        if card.item_definition_code_name == successor_code then return index, card end
    end
    return nil, nil
end

local function find_sacrifice_indexes(state, target_line, target_index, source_card, target_card, settings, helpers)
    local candidates = {}
    if settings.sacrifice_count == 0 then return candidates, nil end

    for _, index in ipairs({ target_index - 1, target_index + 1 }) do
        local card = target_line[index]
        local card_def = card ~= nil and helpers.find_item_def(state.item_defs, card.item_definition_code_name) or nil
        local card_stars = card_def ~= nil and card_def.base_stats ~= nil and tonumber(card_def.base_stats.star) or nil
        local card_race = card_def ~= nil and card_def.metadata ~= nil and card_def.metadata.race or nil
        if card ~= nil and card.inventory_item_id ~= source_card.inventory_item_id and
           card.inventory_item_id ~= target_card.inventory_item_id and
           (settings.sacrifice_star_set == nil or settings.sacrifice_star_set[card_stars] == true) and
           (settings.sacrifice_race == nil or settings.sacrifice_race == card_race) and
           settings.sacrifice_excluded_code_set[card.item_definition_code_name] ~= true then
            table.insert(candidates, { index = index, stars = card_stars })
        end
    end

    table.sort(candidates, function(a, b)
        if a.stars == b.stars then return a.index < b.index end
        return a.stars < b.stars
    end)
    if #candidates < settings.sacrifice_count then
        return nil, settings.ability_key .. " requires " .. settings.sacrifice_count .. " adjacent allied card(s) to sacrifice"
    end

    local indexes = {}
    for index = 1, settings.sacrifice_count do
        indexes[index] = candidates[index].index
    end
    return indexes, nil
end

local function replace_xena_on_line(state, target_line, target_index, void_zone, successor_index, successor_card,
    target_card, sacrifice_indexes, settings, helpers, def_buff)
    table.remove(void_zone, successor_index)
    table.insert(void_zone, target_card)

    successor_card.slot_index = target_card.slot_index
    helpers.lib_battle_common.reset_card_turn_state(state.item_defs, successor_card)
    successor_card.face_up = true
    successor_card.expose = true
    successor_card.defeated_from_line_key = nil
    successor_card.final_def = (successor_card.final_def or 0) + def_buff

    local sacrificed_cards = {}
    for index = 1, settings.sacrifice_count do
        local sacrifice_index = sacrifice_indexes[index]
        local sacrifice_card = target_line[sacrifice_index]
        target_line[sacrifice_index] = {}
        table.insert(void_zone, sacrifice_card)
        table.insert(sacrificed_cards, sacrifice_card)
    end
    target_line[target_index] = successor_card
    return sacrificed_cards
end

local function replace_pending_defenders(state, target_card, successor_card)
    if state.pending_attack ~= nil and
       state.pending_attack.defender_inventory_item_id == target_card.inventory_item_id then
        state.pending_attack.defender_inventory_item_id = successor_card.inventory_item_id
    end
    for _, plans in ipairs({ state.alpha_planning or {}, state.omega_planning or {} }) do
        for _, plan in ipairs(plans) do
            if plan.action == "card_attack_card" and plan.defender_inv_id == target_card.inventory_item_id then
                plan.defender_inv_id = successor_card.inventory_item_id
            end
        end
    end
end

local function build_xena_actions(source_side, source_card, settings, target_card, successor_card,
    sacrificed_cards, target_line_key)
    local actions = {
        source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
            ",ability=" .. settings.ability_key .. ",target=" .. target_card.inventory_item_id ..
            ",summoned=" .. successor_card.inventory_item_id,
    }
    for _, sacrifice_card in ipairs(sacrificed_cards) do
        table.insert(actions, source_side .. "_card_sent_to_void:" .. sacrifice_card.inventory_item_id)
    end
    table.insert(actions, source_side .. "_card_sent_to_void:" .. target_card.inventory_item_id)
    table.insert(actions, source_side .. "_void_to_" .. string.sub(target_line_key, 7) .. ":" ..
        successor_card.inventory_item_id .. "," .. tostring(successor_card.slot_index))
    return actions
end

local function send_source_to_void(state, source_side, source_card, void_zone, battle, actions)
    battle.remove_card_from_line(state[source_side .. "_back_line"], source_card.inventory_item_id)
    table.insert(void_zone, source_card)
    table.insert(actions, source_side .. "_card_sent_to_void:" .. source_card.inventory_item_id)
end

local function execute_xena_awakened(state, source_card, event_data, helpers, config)
    local settings, config_err = validate_xena_config(config)
    if config_err ~= nil then return {}, config_err end

    local target_context, target_err = find_xena_target(state, source_card, event_data, helpers, settings.ability_key)
    if target_err ~= nil then return {}, target_err end

    local battle = helpers.lib_battle_common
    local target_card = target_context.target_card
    local source_side = target_context.source_side
    local void_key = source_side .. "_the_void"
    local void_zone = state[void_key] or {}
    state[void_key] = void_zone

    local incoming_damage = helpers.get_character_incoming_damage(state, target_card)
    if not helpers.is_character_gonna_dead(target_card, incoming_damage) then
        local actions = {
            source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
                ",ability=" .. settings.ability_key .. ",target=" .. target_card.inventory_item_id .. ",result=no_effect",
        }
        send_source_to_void(state, source_side, source_card, void_zone, battle, actions)
        return actions, nil
    end

    local def_buff = tonumber(helpers.get_card_stat(state, source_card, "add_def"))
    if def_buff == nil then def_buff = 0 end

    local target_line_key, target_line, target_index, line_err =
        find_target_line(state, event_data, target_card, settings.ability_key)
    if line_err ~= nil then return {}, line_err end

    local successor_index, successor_card = find_successor_card(void_zone, settings.successor_code)
    if successor_card == nil then
        return {}, settings.ability_key .. " requires " .. settings.successor_name .. " in own the_void"
    end

    local sacrifice_indexes, sacrifice_err = find_sacrifice_indexes(
        state, target_line, target_index, source_card, target_card, settings, helpers)
    if sacrifice_err ~= nil then return {}, sacrifice_err end

    local sacrificed_cards = replace_xena_on_line(state, target_line, target_index, void_zone, successor_index,
        successor_card, target_card, sacrifice_indexes, settings, helpers, def_buff)
    replace_pending_defenders(state, target_card, successor_card)

    local actions = build_xena_actions(source_side, source_card, settings, target_card, successor_card,
        sacrificed_cards, target_line_key)
    send_source_to_void(state, source_side, source_card, void_zone, battle, actions)
    battle.dlog("[ability] " .. settings.ability_key .. ": summoned " .. settings.successor_name .. "=" ..
        successor_card.inventory_item_id .. " to " .. target_line_key .. " slot=" ..
        tostring(successor_card.slot_index) .. " with +" .. def_buff .. " DEF")
    return actions, nil
end

-- ability: xena_awakened1
-- Replaces an attacked Character that will be defeated with Xena II from void.
function xena_awakened1_execute(state, source_card, event_data, helpers)
    return execute_xena_awakened(state, source_card, event_data, helpers, {
        ability_key = "xena_awakened1",
        successor_code = "xena2",
        successor_name = "Xena II",
        sacrifice_count = 0,
    })
end

-- ability: xena_awakened2
-- Replaces an attacked Character that will be defeated with Xena III from void.
function xena_awakened2_execute(state, source_card, event_data, helpers)
    return execute_xena_awakened(state, source_card, event_data, helpers, {
        ability_key = "xena_awakened2",
        successor_code = "xena3",
        successor_name = "Xena III",
        sacrifice_count = 0,
    })
end

-- ability: xena_awakened3
-- Replaces an attacked Xena III that will be defeated with Xena IV from void.
-- Sacrifices the adjacent dark_elf card with the fewest stars (1-3), excluding Xena I-III.
function xena_awakened3_execute(state, source_card, event_data, helpers)
    return execute_xena_awakened(state, source_card, event_data, helpers, {
        ability_key = "xena_awakened3",
        successor_code = "xena4",
        successor_name = "Xena IV",
        sacrifice_count = 1,
        sacrifice_stars = { 1, 2, 3 },
        sacrifice_race = "dark_elf",
        sacrifice_excluded_codes = { "xena1", "xena2", "xena3" },
    })
end
