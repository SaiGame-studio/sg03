-- enemy_ai_goblin_shaman  (is_library = true)
-- AI module for the goblin_shaman enemy.

-- Searches back_line for cards matching code_name.
-- Returns the first exposed card (expose==true) if found; otherwise the first unexposed card.
-- Returns nil if no match exists.
function goblin_shaman_find_back_line_card_prefer_exposed(back_line, code_name)
    lib_battle_common.dlog("[entity_ai] searching back_line (count=" .. #back_line .. ") for code=" .. code_name .. " (prefer exposed)")
    local unexposed_fallback = nil
    for _, back_card in ipairs(back_line) do
        local card_id      = back_card.inventory_item_id or ""
        local card_code    = back_card.item_definition_code_name or ""
        local card_exposed = back_card.expose == true
        lib_battle_common.dlog("[entity_ai] checking back_line card: id=" .. card_id .. " code=" .. card_code .. " expose=" .. tostring(back_card.expose))
        if card_id == "" then
            lib_battle_common.dlog("[entity_ai] skip: no inventory_item_id")
        elseif card_code ~= code_name then
            lib_battle_common.dlog("[entity_ai] skip: code mismatch (want=" .. code_name .. ")")
        elseif card_exposed then
            lib_battle_common.dlog("[entity_ai] found exposed match: id=" .. card_id)
            return back_card
        else
            lib_battle_common.dlog("[entity_ai] found unexposed match (saved as fallback): id=" .. card_id)
            if unexposed_fallback == nil then
                unexposed_fallback = back_card
            end
        end
    end
    if unexposed_fallback ~= nil then
        lib_battle_common.dlog("[entity_ai] using unexposed fallback: id=" .. unexposed_fallback.inventory_item_id)
    end
    return unexposed_fallback
end

-- Returns true if pending_attack targets a card on omega_front_line AND deals positive damage.
function goblin_shaman_is_omega_front_line_taking_damage(state)
    local pending_atk = state.pending_attack
    if pending_atk == nil then
        lib_battle_common.dlog("[entity_ai] is_omega_front_line_taking_damage: no pending_attack")
        return false
    end
    local damage = pending_atk.damage_dealt or 0
    if damage <= 0 then
        lib_battle_common.dlog("[entity_ai] is_omega_front_line_taking_damage: damage_dealt=" .. damage .. " (no damage)")
        return false
    end
    local defender_id      = pending_atk.defender_inventory_item_id or ""
    local omega_front_line = state.omega_front_line or {}
    for _, front_card in ipairs(omega_front_line) do
        if front_card.inventory_item_id == defender_id then
            lib_battle_common.dlog("[entity_ai] is_omega_front_line_taking_damage: defender=" .. defender_id .. " on omega_front_line damage=" .. damage)
            return true
        end
    end
    lib_battle_common.dlog("[entity_ai] is_omega_front_line_taking_damage: defender=" .. defender_id .. " not on omega_front_line, skip")
    return false
end

-- Triggers an on_defend ability on source_card, appends resulting actions into state.
-- Returns err or nil.
function goblin_shaman_trigger_defend_ability(state, source_card, ability_key)
    local source_item_def = nil
    if state.item_defs ~= nil then
        for _, item_def in ipairs(state.item_defs) do
            if item_def.item_code == ability_key then
                source_item_def = item_def
                break
            end
        end
    end
    local def_add = (source_item_def ~= nil and source_item_def.base_stats and source_item_def.base_stats.def_add) or 0
    lib_battle_common.dlog("[entity_ai] trigger_defend_ability: id=" .. source_card.inventory_item_id .. " ability=" .. ability_key .. " def_add=" .. def_add)
    lib_battle_common.dlog("[entity_ai] pending_attack.damage_dealt=" .. tostring(state.pending_attack ~= nil and state.pending_attack.damage_dealt or "nil"))
    local defend_event_data = {}
    defend_event_data.pending_attack = state.pending_attack
    local ability_actions, ability_err = lib_ability_core.trigger_ability_by_key(state, source_card, ability_key, "on_defend", defend_event_data)
    if ability_err ~= nil then
        lib_battle_common.dlog("[entity_ai] ability error: " .. ability_err)
        return ability_err
    end
    lib_battle_common.dlog("[entity_ai] ability_actions count=" .. #ability_actions)
    for _, ability_action in ipairs(ability_actions) do
        lib_battle_common.append_client_action(state, ability_action)
    end
    return nil
end

-- Logs the final_def of every card in the given front line (for post-buff inspection).
function goblin_shaman_log_front_line_def(front_line, label)
    lib_battle_common.dlog("[entity_ai] " .. label .. " front_line def (count=" .. #front_line .. "):")
    for _, front_card in ipairs(front_line) do
        local front_id   = front_card.inventory_item_id or ""
        local front_code = front_card.item_definition_code_name or ""
        local front_def  = front_card.final_def or 0
        lib_battle_common.dlog("[entity_ai]   id=" .. front_id .. " code=" .. front_code .. " final_def=" .. front_def)
    end
end

-- Filters a card list, returning only cards with code_name == "totem_pulse".
function goblin_shaman_filter_totem_pulse_cards(other_cards)
    local totem_pulse_cards = {}
    for _, other_card in ipairs(other_cards) do
        if other_card.item_definition_code_name == "totem_pulse" then
            table.insert(totem_pulse_cards, other_card)
        end
    end
    return totem_pulse_cards
end

-- Defend reaction: trigger totem_pulse from back_line if omega front-line is taking damage.
-- Returns err or nil.
function defend(state)
    lib_battle_common.dlog("[entity_ai] == goblin_shaman.defend ==")
    if not goblin_shaman_is_omega_front_line_taking_damage(state) then
        lib_battle_common.dlog("[entity_ai] goblin_shaman.defend: attack does not damage omega front-line, skip totem")
        return nil
    end
    local omega_back_line = state.omega_back_line or {}
    local totem_card = goblin_shaman_find_back_line_card_prefer_exposed(omega_back_line, "totem_pulse")
    if totem_card == nil then
        lib_battle_common.dlog("[entity_ai] goblin_shaman.defend: no totem_pulse in back_line, skip")
        return nil
    end

    local omega_front_line = state.omega_front_line or {}
    local has_shaman = false
    for _, card in ipairs(omega_front_line) do
        if card.item_definition_code_name == "goblin_shaman" and card.trigger ~= true then
            has_shaman = true
            break
        end
    end
    if not has_shaman then
        lib_battle_common.dlog("[entity_ai] goblin_shaman.defend: no untriggered goblin_shaman in front_line, skip totem_pulse")
        return nil
    end

    local ability_err = goblin_shaman_trigger_defend_ability(state, totem_card, "totem_pulse")
    if ability_err ~= nil then return ability_err end
    goblin_shaman_log_front_line_def(state.omega_front_line or {}, "omega")
    lib_battle_common.dlog("[entity_ai] goblin_shaman.defend done")
    return nil
end

-- Deploy strategy: deploy one character per turn. While Alpha still has a
-- Character on its front line, keep Shaman's next character face-down once one is face-up.
-- Resets newly deployed cards. Returns: front_line, back_line, hand, err.
function deploy(state)
    lib_battle_common.dlog("[entity_ai] == goblin_shaman.deploy ==")

    local slot_count       = lib_battle_common.get_hand_size()
    local omega_front_line = state.omega_front_line or {}
    local omega_back_line  = state.omega_back_line or {}
    local deployed_ids     = {}
    local front_deployed   = {}
    local back_deployed    = {}
    local has_face_up_character = false
    for _, front_card in ipairs(omega_front_line) do
        if front_card.inventory_item_id ~= nil and front_card.inventory_item_id ~= ""
            and front_card.face_up == true
            and lib_battle_common.check_card_type(state.item_defs, front_card, "character") then
            has_face_up_character = true
            break
        end
    end

    local alpha_front_line_character_count = 0
    for _, front_card in ipairs(state.alpha_front_line or {}) do
        if front_card.inventory_item_id ~= nil and front_card.inventory_item_id ~= ""
            and lib_battle_common.check_card_type(state.item_defs, front_card, "character") then
            alpha_front_line_character_count = alpha_front_line_character_count + 1
        end
    end

    local hand_cards                   = lib_battle_ai._collect_cards(state.omega_hand or {})
    local character_cards, other_cards = lib_battle_ai._split_cards_by_type(hand_cards, state.item_defs)
    local totem_pulse_cards            = goblin_shaman_filter_totem_pulse_cards(other_cards)
    lib_battle_common.dlog("[entity_ai] goblin_shaman.deploy: characters=" .. #character_cards .. " totem_pulse=" .. #totem_pulse_cards)

    -- Keep the one-character-per-turn rule. Retain hidden information while
    -- Alpha still has a Character in its front line.
    if #character_cards >= 1 then
        local deploy_card = character_cards[1]
        local face_up = not (has_face_up_character and alpha_front_line_character_count > 0)
        for slot_i = 1, slot_count do
            local existing = omega_front_line[slot_i]
            if existing == nil or existing.item_definition_code_name == nil or existing.item_definition_code_name == "" then
                deploy_card.slot_index   = slot_i - 1
                deploy_card.face_up      = face_up
                deploy_card.expose       = face_up
                omega_front_line[slot_i] = deploy_card
                table.insert(deployed_ids, deploy_card.id)
                table.insert(front_deployed, deploy_card)
                lib_battle_common.dlog("[entity_ai] goblin_shaman.deploy: front slot=" .. (slot_i - 1) .. " card=" .. (deploy_card.inventory_item_id or "?"))
                break
            end
        end
    end

    -- Deploy totem_pulse cards to back (always face down).
    for _, deploy_card in ipairs(totem_pulse_cards) do
        local face_up = false
        for slot_i = 1, slot_count do
            local existing = omega_back_line[slot_i]
            if existing == nil or existing.item_definition_code_name == nil or existing.item_definition_code_name == "" then
                deploy_card.slot_index  = slot_i - 1
                deploy_card.face_up     = face_up
                deploy_card.expose      = face_up
                omega_back_line[slot_i] = deploy_card
                table.insert(deployed_ids, deploy_card.id)
                table.insert(back_deployed, deploy_card)
                lib_battle_common.dlog("[entity_ai] goblin_shaman.deploy: back slot=" .. (slot_i - 1) .. " card=" .. (deploy_card.inventory_item_id or "?"))
                break
            end
        end
    end

    local new_hand = lib_battle_ai._rebuild_hand(state.omega_hand or {}, deployed_ids)
    lib_battle_ai._append_mid_deploy_actions(state, front_deployed, back_deployed)
    lib_battle_ai._reset_deployed_cards(state.item_defs, front_deployed, back_deployed)
    lib_battle_common.dlog("[entity_ai] goblin_shaman.deploy: deployed=" .. #deployed_ids)

    return omega_front_line, omega_back_line, new_hand, nil
end

-- Counts hidden Characters in omega_front_line and returns the first one that
-- may attack. The returned card is intentionally face-down: attacking exposes it.
function goblin_shaman_find_extra_face_down_attacker(state)
    local face_down_count = 0
    local first_face_down_attacker = nil
    for _, front_card in ipairs(state.omega_front_line or {}) do
        local card_id = front_card.inventory_item_id or ""
        if card_id ~= ""
            and front_card.trigger ~= true
            and front_card.face_up ~= true
            and lib_battle_common.check_card_type(state.item_defs, front_card, "character") then
            face_down_count = face_down_count + 1
            if first_face_down_attacker == nil then
                first_face_down_attacker = front_card
            end
        end
    end
    return face_down_count, first_face_down_attacker
end

-- Prefer an exposed Alpha Character on the front line. Within that preferred
-- set, use the lowest DEF and preserve slot order as a deterministic tie-break.
-- If there is no exposed Character, retain the standard target-selection rules.
function goblin_shaman_pick_attack_target(state)
    local selected_card = nil
    local lowest_def = math.huge
    for _, candidate in ipairs(state.alpha_front_line or {}) do
        local candidate_id = candidate.inventory_item_id or ""
        if candidate_id ~= "" and candidate.expose == true and
           lib_battle_common.check_card_type(state.item_defs, candidate, "character") then
            local candidate_def = candidate.final_def or 0
            lib_battle_common.dlog("[entity_ai] goblin_shaman target candidate: exposed character=" ..
                candidate_id .. " final_def=" .. candidate_def)
            if candidate_def < lowest_def then
                selected_card = candidate
                lowest_def = candidate_def
            end
        end
    end
    if selected_card ~= nil then
        lib_battle_common.dlog("[entity_ai] goblin_shaman target selected: exposed character=" ..
            selected_card.inventory_item_id .. " final_def=" .. lowest_def)
        return selected_card
    end
    return lib_battle_ai._pick_alpha_attack_target(state)
end

-- Attack planning: keep one hidden Character. When more than one hidden
-- Character is on the front line, attack with a hidden one to reveal it.
-- Returns err or nil.
function plan_attack(state)
    lib_battle_common.dlog("[entity_ai] == goblin_shaman.plan_attack ==")
    state.omega_planning = {}
    local defender = goblin_shaman_pick_attack_target(state)
    local face_down_count, face_down_attacker = goblin_shaman_find_extra_face_down_attacker(state)
    local attacker = nil
    if face_down_count > 1 then
        attacker = face_down_attacker
        lib_battle_common.dlog("[entity_ai] goblin_shaman.plan_attack: face-down count=" .. face_down_count .. ", attacking with hidden card=" .. attacker.inventory_item_id)
    else
        attacker = lib_battle_ai._find_omega_attacker(state, true)
    end
    if attacker == nil then
        lib_battle_ai.omega_end_turn(state)
        return nil
    end

    if defender == nil then
        table.insert(state.omega_planning, {
            action = "omega_attack_alpha_hp",
            attacker_inv_id = attacker.inventory_item_id,
            defender_inv_id = "alpha_hp"
        })
        lib_battle_common.append_client_action(
            state,
            lib_battle_ai.build_omega_planning_character_attack_action(state, attacker, "alpha_hp")
        )
        return nil
    end

    table.insert(state.omega_planning, {
        action = "card_attack_card",
        attacker_inv_id = attacker.inventory_item_id,
        defender_inv_id = defender.inventory_item_id
    })
    lib_battle_common.append_client_action(
        state,
        lib_battle_ai.build_omega_planning_character_attack_action(state, attacker, defender.inventory_item_id)
    )
    return nil
end
