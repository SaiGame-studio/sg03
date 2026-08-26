-- battle_session_exists
-- Reports whether the current player has an active battle session.

local session_id, err = game.battle_session_current_id()
if err ~= nil then
    local error_text = string.lower(tostring(err))
    local session_not_found = error_text == "no active battle session found"
        or error_text == "no active battle session"
        or error_text == "current battle session not found"
        or error_text == "battle session not found"
    if session_not_found then
        output.exists = false
        return
    end

    output.error = err
    return
end

if session_id == nil or session_id == "" then
    output.exists = false
    return
end

output.exists = true
output.session_id = session_id
