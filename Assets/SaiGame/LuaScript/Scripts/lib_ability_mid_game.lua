-- lib_ability_mid_game
-- is_library = true

-- ability: titan_fall
-- Replaces a threatened Human with Titan before the queued opposing attack resolves.
function titan_fall_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    local target_card = (event_data or {}).defender_card
    if target_card == nil then
        return {}, "titan_fall requires a Human target card"
    end

    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return {}, "titan_fall source card is not on a battle line"
    end

    local target_line_key = (event_data or {}).defender_line_key
    local target_line = target_line_key ~= nil and state[target_line_key] or nil
    local target_index = nil
    if target_line ~= nil then
        for i, card in ipairs(target_line) do
            if card.inventory_item_id == target_card.inventory_item_id then
                target_index = i
                break
            end
        end
    end
    if target_index == nil then
        return {}, "titan_fall target must be on a battle line"
    end

    local target_def = helpers.find_item_def(state.item_defs, target_card.item_definition_code_name)
    local target_type = target_def ~= nil and target_def.metadata ~= nil and target_def.metadata.type or nil
    local target_race = target_def ~= nil and target_def.metadata ~= nil and target_def.metadata.race or nil
    if target_type ~= "character" or target_race ~= "human" then
        return {}, "titan_fall target must be a Human character"
    end

    local base_def = target_def.base_stats ~= nil and target_def.base_stats.def or 0
    local final_def = target_card.final_def or base_def
    local def_buff = final_def - base_def
    local titan_fall_def = helpers.find_item_def(state.item_defs, source_card.item_definition_code_name)
    local def_buff_required = titan_fall_def ~= nil and titan_fall_def.base_stats ~= nil
        and titan_fall_def.base_stats.def_buff_required or 0
    if def_buff_required <= 0 then
        return {}, "titan_fall requires a positive base_stats.def_buff_required"
    end
    if def_buff < def_buff_required then
        return {}, "titan_fall target requires at least +" .. def_buff_required .. " DEF"
    end

    local opponent_planning = source_side == "alpha" and (state.omega_planning or {}) or (state.alpha_planning or {})
    local attack_plan = nil
    for _, plan in ipairs(opponent_planning) do
        if plan.action == "card_attack_card" and plan.defender_inv_id == target_card.inventory_item_id then
            attack_plan = plan
            break
        end
    end
    if attack_plan == nil then
        return {}, "titan_fall target is not being attacked"
    end

    local attacker_card = nil
    local attacker_def = nil
    local opponent_lines = source_side == "alpha"
        and { state.omega_front_line or {}, state.omega_back_line or {} }
        or { state.alpha_front_line or {}, state.alpha_back_line or {} }
    for _, line in ipairs(opponent_lines) do
        for _, card in ipairs(line) do
            if card.inventory_item_id == attack_plan.attacker_inv_id then
                attacker_card = card
                attacker_def = helpers.find_item_def(state.item_defs, card.item_definition_code_name)
                break
            end
        end
        if attacker_card ~= nil then break end
    end
    if attacker_card == nil or attacker_def == nil then
        return {}, "titan_fall attacking card was not found"
    end

    local attacker_atk = attacker_def.base_stats ~= nil and attacker_def.base_stats.atk or 0
    local accumulated_damage = target_card.total_damage_received or 0
    local total_attack_damage = attacker_atk + accumulated_damage
    if total_attack_damage < final_def then
        local remaining_def = final_def - total_attack_damage
        return {}, "titan_fall cannot trigger: target Human would survive with " .. remaining_def .. " DEF remaining"
    end
    if total_attack_damage == final_def then
        return {}, "titan_fall cannot trigger: attack equals target DEF; Titan Fall requires damage to exceed DEF"
    end

    local ren_card = helpers.find_line_card_by_code(state[source_side .. "_front_line"], "azure_blade")
    if ren_card == nil then
        ren_card = helpers.find_line_card_by_code(state[source_side .. "_back_line"], "azure_blade")
    end
    if ren_card == nil then
        return {}, "titan_fall requires azure_blade on the field"
    end

    local void_key = source_side .. "_the_void"
    local titan_card = nil
    local titan_index = nil
    for i, card in ipairs(state[void_key] or {}) do
        if card.item_definition_code_name == "titan" then
            titan_card = card
            titan_index = i
            break
        end
    end
    if titan_card == nil then
        return {}, "titan_fall requires titan in the_void"
    end

    local target_slot_index = target_card.slot_index
    table.remove(state[void_key], titan_index)
    table.insert(state[void_key], target_card)
    battle.reset_card_turn_state(state.item_defs, titan_card)
    titan_card.slot_index = target_slot_index
    titan_card.face_up = true
    titan_card.expose = true
    titan_card.trigger = true
    target_line[target_index] = titan_card
    attack_plan.defender_inv_id = titan_card.inventory_item_id

    battle.dlog("[ability] titan_fall: target=" .. target_card.inventory_item_id .. " def_buff=" .. def_buff .. " def_buff_required=" .. def_buff_required .. " attacker_atk=" .. attacker_atk .. " accumulated_damage=" .. accumulated_damage .. " titan=" .. titan_card.inventory_item_id)
    local expose_ren = helpers.expose_ability_selected_card(state, ren_card)
    local actions = {}
    if expose_ren ~= nil then table.insert(actions, expose_ren) end
    table.insert(actions, source_side .. "_card_ability:source=" .. source_card.inventory_item_id .. ",ability=titan_fall,target=" .. target_card.inventory_item_id .. ",selected=" .. ren_card.inventory_item_id)
    table.insert(actions, source_side .. "_card_sent_to_void:" .. target_card.inventory_item_id)
    table.insert(actions, source_side .. "_void_to_front_line:" .. titan_card.inventory_item_id .. "," .. tostring(target_slot_index))
    return actions, nil
end
