-- lib_ability_character_passives
-- is_library = true

-- passive: scout_strike (Lyra)
-- After Lyra attacks, expose one face-down card directly adjacent to the
-- attacked target on the same battle line. Prefer the left neighbour so the
-- result is deterministic when both neighbours are eligible.
function scout_strike_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    local defender = (event_data or {}).defender_card
    local defender_line_key = (event_data or {}).defender_line_key
    local defender_line = defender_line_key ~= nil and state[defender_line_key] or nil

    if defender == nil or defender_line == nil then
        battle.dlog("[ability] scout_strike: skip - target or target line is unavailable")
        return {}, nil
    end

    local defender_slot = defender.slot_index
    if defender_slot == nil then
        battle.dlog("[ability] scout_strike: skip - target has no slot_index")
        return {}, nil
    end

    local target = nil
    for _, adjacent_slot in ipairs({ defender_slot - 1, defender_slot + 1 }) do
        for _, line_card in ipairs(defender_line) do
            if line_card.inventory_item_id ~= nil and line_card.inventory_item_id ~= ""
                and line_card.slot_index == adjacent_slot
                and line_card.face_up ~= true
                and line_card.expose ~= true then
                target = line_card
                break
            end
        end
        if target ~= nil then break end
    end

    if target == nil then
        battle.dlog("[ability] scout_strike: no face-down adjacent card, skip")
        return {}, nil
    end

    target.face_up = true
    target.expose = true
    local target_side = helpers.find_card_side(state, target)
    local source_side = helpers.find_card_side(state, source_card)
    battle.dlog("[ability] scout_strike: exposed target=" .. target.inventory_item_id)
    return {
        target_side .. "_card_expose:" .. target.inventory_item_id,
        source_side .. "_card_ability:source=" .. source_card.inventory_item_id .. ",ability=scout_strike,target=" .. target.inventory_item_id
    }, nil
end
