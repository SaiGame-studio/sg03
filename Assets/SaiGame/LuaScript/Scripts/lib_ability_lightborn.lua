function holy_glow_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    battle.dlog("== [ability] holy_glow ====================")

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
            return "alpha" -- fallback
        end
        caster_side = find_side_in_all_zones(source_card.inventory_item_id)
    end

    local frontline_key = caster_side .. "_front_line"
    local front_line = state[frontline_key] or {}
    local lightborn_female_card = helpers.find_untriggered_card(front_line, function(c)
        local def = helpers.find_item_def(state.item_defs, c.item_definition_code_name)
        local is_lightborn = def ~= nil and def.metadata ~= nil and
            (def.metadata.race == "lightborn" or def.metadata.race == "light_elf")
        return def ~= nil and def.metadata ~= nil and
            def.metadata.type == "character" and
            is_lightborn and
            def.metadata.gender == "female"
    end)

    if lightborn_female_card == nil then
        battle.dlog("[ability] holy_glow: error - no untriggered female Lightborn character in " .. frontline_key)
        return {}, "holy_glow requires an untriggered female Lightborn character in front_line"
    end

    lightborn_female_card.trigger = true
    local expose_action = helpers.expose_ability_selected_card(state, lightborn_female_card)

    local hp_key = caster_side .. "_hp"
    local max_hp_key = caster_side .. "_max_hp"

    -- Ensure max HP is declared for the battle
    if state.alpha_max_hp == nil then
        state.alpha_max_hp = state.alpha_hp
    end
    if state.omega_max_hp == nil then
        state.omega_max_hp = state.omega_hp
    end

    local card_def = helpers.find_item_def(state.item_defs, source_card.item_definition_code_name)
    local hp_restore = 0
    if card_def ~= nil and card_def.base_stats ~= nil and card_def.base_stats.hp_restore ~= nil then
        hp_restore = card_def.base_stats.hp_restore
    end

    local current_hp = state[hp_key] or 0
    local max_hp = state[max_hp_key] or current_hp
    local new_hp = current_hp + hp_restore
    if new_hp > max_hp then
        new_hp = max_hp
    end
    local actual_restored = new_hp - current_hp
    state[hp_key] = new_hp

    local is_ability_card = (card_def ~= nil and card_def.metadata ~= nil and card_def.metadata.type == "ability")
    local will_system_send_to_void = false
    if is_ability_card then
        local defender_line_key = event_data ~= nil and event_data.defender_line_key or nil
        if defender_line_key == "alpha_front_line" or
           defender_line_key == "alpha_back_line"  or
           defender_line_key == "omega_front_line" or
           defender_line_key == "omega_back_line" then
            will_system_send_to_void = true
        end
    end

    battle.dlog("[ability] holy_glow: caster=" .. source_card.inventory_item_id .. " side=" .. caster_side .. " lightborn_female=" .. lightborn_female_card.inventory_item_id .. " restore=" .. hp_restore .. " actual_restored=" .. actual_restored .. " new_hp=" .. new_hp .. "/" .. max_hp .. " will_system_send_to_void=" .. tostring(will_system_send_to_void))

    if not will_system_send_to_void then
        -- Move source card to the void
        local lines_to_check = {
            caster_side .. "_front_line",
            caster_side .. "_back_line",
            caster_side .. "_hand"
        }
        for _, line_key in ipairs(lines_to_check) do
            local line = state[line_key]
            if line ~= nil then
                battle.remove_card_from_line(line, source_card.inventory_item_id)
            end
        end

        local void_key = caster_side .. "_the_void"
        if state[void_key] == nil then state[void_key] = {} end
        table.insert(state[void_key], source_card)
        battle.dlog("[ability] holy_glow: source card sent to void=" .. void_key .. " id=" .. source_card.inventory_item_id)
    end

    local ability_actions = {}
    if expose_action ~= nil then
        table.insert(ability_actions, expose_action)
    end
    table.insert(ability_actions, caster_side .. "_card_ability:source=" .. source_card.inventory_item_id .. ",ability=holy_glow,hp_restore=" .. hp_restore .. ",actual_restored=" .. actual_restored .. "," .. hp_key .. "=" .. state[hp_key] .. ",selected=" .. lightborn_female_card.inventory_item_id)
    
    if not will_system_send_to_void then
        table.insert(ability_actions, caster_side .. "_card_sent_to_void:" .. source_card.inventory_item_id)
    end

    return ability_actions, nil
end

-- ability: skeleton_shield
