function skeleton_shield_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    battle.dlog("== [ability] skeleton_shield ====================")

    local target_card = (event_data or {}).defender_card
    if target_card == nil then
        battle.dlog("[ability] skeleton_shield: skip - defender_card is nil in event_data")
        return {}, nil
    end

    local source_side = helpers.find_card_side(state, source_card)
    local front_line_key = source_side .. "_front_line"
    local front_line = state[front_line_key] or {}

    -- Requirement 1: Ria must be present in front_line, even if triggered.
    local ria_card = nil
    for _, card in ipairs(front_line) do
        local has_id = card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
        if has_id and card.item_definition_code_name == "ria" then
            ria_card = card
            break
        end
    end
    if ria_card == nil then
        battle.dlog("[ability] skeleton_shield: error - no ria in " .. front_line_key)
        return {}, "skeleton_shield requires ria in front_line"
    end

    -- Requirement 2: Must have a skeleton card in front_line (different from target_card)
    local skeleton_card = nil
    local skel_idx = nil
    for i, c in ipairs(front_line) do
        local has_id = c.inventory_item_id ~= nil and c.inventory_item_id ~= ""
        if has_id and c.item_definition_code_name == "skeleton" and c.inventory_item_id ~= target_card.inventory_item_id then
            skeleton_card = c
            skel_idx = i
            break
        end
    end
    if skeleton_card == nil or skel_idx == nil then
        battle.dlog("[ability] skeleton_shield: error - no distinct skeleton in " .. front_line_key)
        return {}, "skeleton_shield requires skeleton in front_line different from target_card"
    end

    -- Requirement 3: Target card must be currently targeted by an opponent planning attack
    local opponent_planning = (source_side == "alpha") and (state.omega_planning or {}) or (state.alpha_planning or {})
    local target_plan_entry = nil
    for _, plan_entry in ipairs(opponent_planning) do
        if plan_entry.defender_inv_id == target_card.inventory_item_id then
            target_plan_entry = plan_entry
            break
        end
    end

    if target_plan_entry == nil then
        battle.dlog("[ability] skeleton_shield: error - target_card is not targeted by opponent planning attack")
        return {}, "skeleton_shield requires target card to be targeted by opponent planning attack"
    end

    -- Find target_card line and slot index
    local target_line_key = (event_data or {}).defender_line_key
    if target_line_key == nil or target_line_key == "" or state[target_line_key] == nil then
        if state.alpha_front_line then
            for _, c in ipairs(state.alpha_front_line) do
                if c.inventory_item_id == target_card.inventory_item_id then
                    target_line_key = "alpha_front_line"
                    break
                end
            end
        end
        if target_line_key == nil and state.omega_front_line then
            for _, c in ipairs(state.omega_front_line) do
                if c.inventory_item_id == target_card.inventory_item_id then
                    target_line_key = "omega_front_line"
                    break
                end
            end
        end
    end

    local target_line = target_line_key ~= nil and state[target_line_key] or nil
    local target_idx = nil
    if target_line ~= nil then
        for i, c in ipairs(target_line) do
            if c.inventory_item_id == target_card.inventory_item_id then
                target_idx = i
                break
            end
        end
    end

    if target_line == nil or target_idx == nil then
        battle.dlog("[ability] skeleton_shield: error - target_card position not found")
        return {}, "skeleton_shield target_card position not found"
    end

    -- Swap position of skeleton_card and target_card
    local temp_slot = skeleton_card.slot_index
    skeleton_card.slot_index = target_card.slot_index
    target_card.slot_index = temp_slot

    if front_line_key == target_line_key then
        front_line[skel_idx] = target_card
        front_line[target_idx] = skeleton_card
    else
        front_line[skel_idx] = target_card
        target_line[target_idx] = skeleton_card
    end

    -- Redirect the opponent's planned attack to the skeleton card (as a substitute shield)
    target_plan_entry.defender_inv_id = skeleton_card.inventory_item_id

    ria_card.trigger = true


    local expose_action = helpers.expose_ability_selected_card(state, ria_card)
    battle.dlog("[ability] skeleton_shield: swapped skeleton=" .. skeleton_card.inventory_item_id .. " and target=" .. target_card.inventory_item_id)

    local shield_actions = {}
    if expose_action ~= nil then
        table.insert(shield_actions, expose_action)
    end
    table.insert(shield_actions, source_side .. "_card_ability:source=" .. source_card.inventory_item_id .. ",ability=skeleton_shield,target=" .. target_card.inventory_item_id .. ",selected=" .. ria_card.inventory_item_id .. ",swapped=" .. skeleton_card.inventory_item_id)
    table.insert(shield_actions, source_side .. "_card_swapped:card1=" .. skeleton_card.inventory_item_id .. ",card2=" .. target_card.inventory_item_id)
    table.insert(shield_actions, source_side .. "_card_guarded:" .. target_card.inventory_item_id)

    return shield_actions, nil
end

local function abyssal_mist_find_untriggered_misthy(state, source_side, helpers)
    for _, line_key in ipairs({ source_side .. "_front_line", source_side .. "_back_line" }) do
        local misthy_card = helpers.find_untriggered_card(state[line_key], function(card)
            return card.item_definition_code_name == "misthy"
        end)
        if misthy_card ~= nil then return misthy_card end
    end
    return nil
end

local function abyssal_mist_apply_def_bonus(state, source_card, def_added, helpers, actions)
    local source_id = source_card.inventory_item_id
    for _, line_data in ipairs({
        { side = "alpha", line = state.alpha_front_line or {} },
        { side = "alpha", line = state.alpha_back_line or {} },
        { side = "omega", line = state.omega_front_line or {} },
        { side = "omega", line = state.omega_back_line or {} },
    }) do
        for _, target_card in ipairs(line_data.line) do
            local has_id = target_card.inventory_item_id ~= nil and target_card.inventory_item_id ~= ""
            local target_def = has_id and helpers.find_item_def(state.item_defs, target_card.item_definition_code_name) or nil
            local target_type = target_def ~= nil and target_def.metadata ~= nil and target_def.metadata.type or nil
            local target_race = target_def ~= nil and target_def.metadata ~= nil and target_def.metadata.race or nil
            if target_type == "character" and (target_race == "darkborn" or target_race == "natureborn") then
                target_card.persistent_def_bonuses = target_card.persistent_def_bonuses or {}
                target_card.persistent_def_bonuses[source_id] = def_added
                target_card.final_def = (target_card.final_def or 0) + def_added
                table.insert(actions, line_data.side .. "_card_ability:source=" .. source_id ..
                    ",ability=abyssal_mist,target=" .. target_card.inventory_item_id)
            end
        end
    end
end

-- Removes the bonuses created by one Abyssal Mist. A future card that destroys
-- the field card must call this before moving source_card out of its battle line.
function abyssal_mist_remove_effects(state, source_card, helpers)
    if source_card == nil or source_card.inventory_item_id == nil or source_card.inventory_item_id == "" then return end
    local source_id = source_card.inventory_item_id
    for _, line in ipairs({
        state.alpha_front_line or {}, state.alpha_back_line or {},
        state.omega_front_line or {}, state.omega_back_line or {},
    }) do
        for _, card in ipairs(line) do
            if card.persistent_def_bonuses ~= nil and card.persistent_def_bonuses[source_id] ~= nil then
                local def_added = tonumber(card.persistent_def_bonuses[source_id]) or 0
                card.persistent_def_bonuses[source_id] = nil
                card.final_def = math.max(0, (card.final_def or 0) - def_added)
            end
            if card.persistent_atk_bonuses ~= nil then
                card.persistent_atk_bonuses[source_id] = nil
            end
        end
    end
    source_card.abyssal_mist_active = nil
end

function abyssal_mist_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return {}, "abyssal_mist source card is not on the battlefield"
    end
    if source_card.abyssal_mist_active == true then
        return {}, "abyssal_mist is already active"
    end

    local misthy_card = abyssal_mist_find_untriggered_misthy(state, source_side, helpers)
    if misthy_card == nil then
        return {}, "abyssal_mist requires untriggered misthy on the battlefield"
    end

    local atk_added = tonumber(helpers.get_card_stat(state, source_card, "atk_added"))
    local def_added = tonumber(helpers.get_card_stat(state, source_card, "def_added"))
    if atk_added == nil or atk_added <= 0 or def_added == nil or def_added <= 0 then
        return {}, "abyssal_mist requires positive base_stats.atk_added and base_stats.def_added"
    end

    local actions = {}
    local expose_action = helpers.expose_ability_selected_card(state, misthy_card)
    if expose_action ~= nil then table.insert(actions, expose_action) end
    misthy_card.trigger = true
    misthy_card.persistent_atk_bonuses = misthy_card.persistent_atk_bonuses or {}
    misthy_card.persistent_atk_bonuses[source_card.inventory_item_id] = atk_added
    source_card.abyssal_mist_active = true

    abyssal_mist_apply_def_bonus(state, source_card, def_added, helpers, actions)
    table.insert(actions, source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
        ",ability=abyssal_mist,selected=" .. misthy_card.inventory_item_id)
    battle.dlog("[ability] abyssal_mist: source=" .. source_card.inventory_item_id ..
        " misthy=" .. misthy_card.inventory_item_id .. " atk_added=" .. atk_added .. " def_added=" .. def_added)
    return actions, nil
end
