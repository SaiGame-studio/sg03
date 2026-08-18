-- lib_ability_config
-- is_library = true

-- Valid target_positions:
--   own_frontline, own_backline, own_hand, own_void, own_source
--   enemy_frontline, enemy_backline, enemy_hand, enemy_void, enemy_source
--
-- Các trường cấu hình Ability:
--   event (string, không bắt buộc): Sự kiện mà dispatcher được phép kích hoạt.
--   target_positions (string[], không bắt buộc): Các vùng mục tiêu hợp lệ.
--   requires_target_card (boolean, không bắt buộc): true khi Ability phải có
--       một lá bài mục tiêu cụ thể, không thể dùng lên người chơi/HP.
--   resolves_without_attack (boolean, không bắt buộc): true khi lá Ability
--       chỉ thực hiện hiệu ứng, không đi qua luồng tấn công và gây sát thương.
function get_ability_config(ability_key)
    local configs = {
        twin_reaper = {
            event = "on_attack",
            target_positions = { "enemy_frontline" },
        },
        scout_strike = {
            event = "on_attack",
            target_positions = { "enemy_frontline" },
        },
        eagle_eye = {
            target_positions = { "enemy_frontline" },
            requires_target_card = true,
            resolves_without_attack = true,
        },
        spinning_slash = {
            target_positions = { "enemy_frontline" },
            requires_target_card = true,
        },
        cross_guard = {
            target_positions = { "own_frontline" },
            requires_target_card = true,
        },
        totem_pulse = {
            target_positions = { "own_frontline" },
        },
        back_stab = {
            target_positions = { "enemy_frontline" },
            requires_target_card = true,
        },
        holy_glow = {
            target_positions = { "own_frontline", "own_backline", "own_source", "own_void" },
        },
        skeleton_shield = {
            target_positions = { "own_frontline" },
            requires_target_card = true,
        },
        animate_dead = {
            target_positions = { "own_frontline", "own_backline", "own_source", "own_void" },
        },
    }
    return configs[ability_key]
end
