-- battle_session_exists
-- Reports whether the current player has an active battle session.

local session_id, err = game.battle_session_current_id()
if err ~= nil then
    output.error = err
    return
end

if session_id == nil or session_id == "" then
    output.exists = false
    return
end

output.exists = true
output.session_id = session_id
