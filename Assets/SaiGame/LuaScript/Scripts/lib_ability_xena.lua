-- lib_ability_xena
-- is_library = true

-- ability: xena_awakened1
-- Target the Xena I that was just defeated and is now in the caster's void.
-- The replacement is deliberately selected in void order because this action
-- has no second target parameter for choosing among multiple Xena II cards.
function xena_awakened1_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    local target_card = (event_data or {}).defender_card
    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return {}, "xena_awakened1 source card is not on a battle line"
    end
    if target_card == nil or target_card.item_definition_code_name ~= "xena1" then
        return {}, "xena_awakened1 requires Xena I as its target"
    end

    local void_key = source_side .. "_the_void"
    local void_zone = state[void_key] or {}
    local target_index = nil
    for i, card in ipairs(void_zone) do
        if card.inventory_item_id == target_card.inventory_item_id then
            target_index = i
            break
        end
    end
    if target_index == nil then
        -- The attack has resolved but Xena I survived. The ability is still
        -- consumed, as specified, but it cannot summon a replacement.
        local ability_actions = {
            source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
                ",ability=xena_awakened1,target=" .. target_card.inventory_item_id .. ",result=no_effect",
        }
        for _, line_key in ipairs({ source_side .. "_front_line", source_side .. "_back_line" }) do
            battle.remove_card_from_line(state[line_key], source_card.inventory_item_id)
        end
        table.insert(void_zone, source_card)
        table.insert(ability_actions, source_side .. "_card_sent_to_void:" .. source_card.inventory_item_id)
        return ability_actions, nil
    end
    if (target_card.total_damage_received or 0) <= 0 or
       (target_card.total_damage_received or 0) < (target_card.final_def or 0) then
        return {}, "xena_awakened1 requires Xena I to be defeated by damage"
    end

    local destination_key = target_card.defeated_from_line_key
    local destination_line = destination_key ~= nil and state[destination_key] or nil
    local slot_index = target_card.slot_index
    if destination_line == nil or slot_index == nil or slot_index < 0 then
        return {}, "xena_awakened1 cannot determine Xena I's former position"
    end
    local line_slot = slot_index + 1
    local occupied = destination_line[line_slot]
    if occupied ~= nil and occupied.inventory_item_id ~= nil and occupied.inventory_item_id ~= "" then
        return {}, "xena_awakened1 former position is no longer empty"
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
    successor_card.slot_index = slot_index
    successor_card.face_up = true
    successor_card.expose = true
    successor_card.defeated_from_line_key = nil
    successor_card.final_def = (successor_card.final_def or 0) + 100
    destination_line[line_slot] = successor_card

    local ability_actions = {
        source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
            ",ability=xena_awakened1,target=" .. target_card.inventory_item_id ..
            ",summoned=" .. successor_card.inventory_item_id,
        source_side .. "_void_to_" .. string.sub(destination_key, 7) .. ":" ..
            successor_card.inventory_item_id .. "," .. slot_index,
    }

    for _, line_key in ipairs({ source_side .. "_front_line", source_side .. "_back_line" }) do
        battle.remove_card_from_line(state[line_key], source_card.inventory_item_id)
    end
    table.insert(void_zone, source_card)
    table.insert(ability_actions, source_side .. "_card_sent_to_void:" .. source_card.inventory_item_id)
    battle.dlog("[ability] xena_awakened1: summoned Xena II=" .. successor_card.inventory_item_id ..
        " to " .. destination_key .. " slot=" .. slot_index .. " with +100 DEF")

    return ability_actions, nil
end
