-- lib_ability_xena
-- is_library = true

-- ability: xena_awakened1
-- Replace an attacked Character that will be defeated with Xena II from void.
function xena_awakened1_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    local target_card = (event_data or {}).defender_card
    if target_card == nil then
        return {}, "xena_awakened1 requires a target card"
    end

    local target_def = helpers.find_item_def(state.item_defs, target_card.item_definition_code_name)
    local target_type = target_def ~= nil and target_def.metadata ~= nil and target_def.metadata.type or nil
    if target_type ~= "character" then
        return {}, "xena_awakened1 target must be a Character"
    end

    if not helpers.is_character_be_attacked(state, target_card) then
        return {}, "xena_awakened1 target is not being attacked"
    end

    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return {}, "xena_awakened1 source card is not on a battle line"
    end

    local void_key = source_side .. "_the_void"
    local void_zone = state[void_key] or {}
    state[void_key] = void_zone

    local incoming_damage = helpers.get_character_incoming_damage(state, target_card)
    if not helpers.is_character_gonna_dead(target_card, incoming_damage) then
        local actions = {
            source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
                ",ability=xena_awakened1,target=" .. target_card.inventory_item_id .. ",result=no_effect",
        }
        battle.remove_card_from_line(state[source_side .. "_back_line"], source_card.inventory_item_id)
        table.insert(void_zone, source_card)
        table.insert(actions, source_side .. "_card_sent_to_void:" .. source_card.inventory_item_id)
        return actions, nil
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
        return {}, "xena_awakened1 target must be on a battle line"
    end

    local successor_index = nil
    local successor_card = nil
    for i, card in ipairs(void_zone) do
        if card.item_definition_code_name == "xena2" then
            successor_index = i
            successor_card = card
            break
        end
    end
    if successor_card == nil then
        return {}, "xena_awakened1 requires Xena II in own the_void"
    end

    table.remove(void_zone, successor_index)
    table.insert(void_zone, target_card)
    successor_card.slot_index = target_card.slot_index
    battle.reset_card_turn_state(state.item_defs, successor_card)
    successor_card.face_up = true
    successor_card.expose = true
    successor_card.defeated_from_line_key = nil
    successor_card.final_def = (successor_card.final_def or 0) + 100
    target_line[target_index] = successor_card

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

    local ability_actions = {
        source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
            ",ability=xena_awakened1,target=" .. target_card.inventory_item_id ..
            ",summoned=" .. successor_card.inventory_item_id,
        source_side .. "_card_sent_to_void:" .. target_card.inventory_item_id,
        source_side .. "_void_to_" .. string.sub(target_line_key, 7) .. ":" ..
            successor_card.inventory_item_id .. "," .. tostring(successor_card.slot_index),
    }

    battle.remove_card_from_line(state[source_side .. "_back_line"], source_card.inventory_item_id)
    table.insert(void_zone, source_card)
    table.insert(ability_actions, source_side .. "_card_sent_to_void:" .. source_card.inventory_item_id)
    battle.dlog("[ability] xena_awakened1: summoned Xena II=" .. successor_card.inventory_item_id ..
        " to " .. target_line_key .. " slot=" .. tostring(successor_card.slot_index) .. " with +100 DEF")

    return ability_actions, nil
end
