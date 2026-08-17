-- lib_ability_advanced
-- is_library = true

-- ability: animate_dead
function animate_dead_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    battle.dlog("== [ability] animate_dead ====================")

    local caster_side = helpers.find_card_side(state, source_card)
    if caster_side == "unknown" or caster_side == nil then
        local function find_side_in_all_zones(card_id)
            local alpha_keys = { "alpha_front_line", "alpha_back_line", "alpha_hand", "alpha_the_void", "alpha_the_source" }
            for _, key in ipairs(alpha_keys) do
                if state[key] then
                    for _, c in ipairs(state[key]) do
                        if c.inventory_item_id == card_id then return "alpha" end
                    end
                end
            end
            local omega_keys = { "omega_front_line", "omega_back_line", "omega_hand", "omega_the_void", "omega_the_source" }
            for _, key in ipairs(omega_keys) do
                if state[key] then
                    for _, c in ipairs(state[key]) do
                        if c.inventory_item_id == card_id then return "omega" end
                    end
                end
            end
            return "alpha"
        end
        caster_side = find_side_in_all_zones(source_card.inventory_item_id)
    end

    local front_line_key = caster_side .. "_front_line"
    local front_line = state[front_line_key] or {}
    local hellscythe_card = helpers.find_untriggered_card(front_line, function(c)
        return c.item_definition_code_name == "hellscythe"
    end)
    if hellscythe_card == nil then
        battle.dlog("[ability] animate_dead: error - no untriggered hellscythe in " .. front_line_key)
        return {}, "animate_dead requires untriggered hellscythe in front_line"
    end

    hellscythe_card.trigger = true
    local expose_action = helpers.expose_ability_selected_card(state, hellscythe_card)
    local ability_actions = {}
    if expose_action ~= nil then table.insert(ability_actions, expose_action) end
    table.insert(ability_actions, caster_side .. "_card_ability:source=" .. source_card.inventory_item_id .. ",ability=animate_dead,selected=" .. hellscythe_card.inventory_item_id)

    local void_key = caster_side .. "_the_void"
    local void_zone = state[void_key] or {}
    for _ = 1, 3 do
        local skeleton_idx = nil
        local skeleton_card = nil
        for i, c in ipairs(void_zone) do
            if c.item_definition_code_name == "skeleton" then
                skeleton_idx = i
                skeleton_card = c
                break
            end
        end
        if skeleton_card == nil then
            battle.dlog("[ability] animate_dead: no more skeleton in " .. void_key)
            break
        end

        local free_slots = {}
        for i = 1, 5 do
            local slot = front_line[i]
            if slot == nil or slot.inventory_item_id == nil or slot.inventory_item_id == "" then
                table.insert(free_slots, i)
            end
        end
        if #free_slots == 0 then
            battle.dlog("[ability] animate_dead: front_line has no free slots, stopping summon")
            break
        end

        local chosen_slot_idx = free_slots[math.random(1, #free_slots)]
        table.remove(void_zone, skeleton_idx)
        skeleton_card.slot_index = chosen_slot_idx - 1
        battle.reset_card_turn_state(state.item_defs, skeleton_card)
        skeleton_card.trigger = true
        skeleton_card.face_up = true
        skeleton_card.expose = true
        front_line[chosen_slot_idx] = skeleton_card
        battle.dlog("[ability] animate_dead: summoned skeleton=" .. skeleton_card.inventory_item_id .. " to slot=" .. skeleton_card.slot_index)
        table.insert(ability_actions, caster_side .. "_void_to_front_line:" .. skeleton_card.inventory_item_id .. "," .. skeleton_card.slot_index)
    end

    local card_def = helpers.find_item_def(state.item_defs, source_card.item_definition_code_name)
    local is_ability_card = card_def ~= nil and card_def.metadata ~= nil and card_def.metadata.type == "ability"
    local defender_line_key = event_data ~= nil and event_data.defender_line_key or nil
    local will_system_send_to_void = is_ability_card and (
        defender_line_key == "alpha_front_line" or defender_line_key == "alpha_back_line" or
        defender_line_key == "omega_front_line" or defender_line_key == "omega_back_line"
    )
    if not will_system_send_to_void then
        for _, line_key in ipairs({ caster_side .. "_front_line", caster_side .. "_back_line", caster_side .. "_hand" }) do
            local line = state[line_key]
            if line ~= nil then battle.remove_card_from_line(line, source_card.inventory_item_id) end
        end
        if state[void_key] == nil then state[void_key] = {} end
        table.insert(state[void_key], source_card)
        battle.dlog("[ability] animate_dead: source card sent to void=" .. void_key .. " id=" .. source_card.inventory_item_id)
        table.insert(ability_actions, caster_side .. "_card_sent_to_void:" .. source_card.inventory_item_id)
    end

    return ability_actions, nil
end
