# 04. Hệ Thống Kỹ Năng & Hướng Dẫn Thực Thi

## 1. Kiến Trúc Điều Hướng Kỹ Năng Cốt Lõi

Các kỹ năng trong **_sg03** được quản lý bởi 2 module Lua cốt lõi:
- `lib_ability_core.lua`: Xử lý xác minh mục tiêu, kiểm tra khu vực vị trí, lật mặt bài, áp dụng sát thương và điều hướng thực thi.
- `lib_ability_all.lua`: Chứa logic thực thi chi tiết và các hàm trợ giúp cho từng mã kỹ năng cụ thể.

### Quy Tắc Kiểm Tra Vị Trí Nhắm Mục Tiêu

Khi một kỹ năng được gọi, `can_ability_target_position` kiểm tra thẻ bài hoặc hàng mục tiêu xem có khớp với các vị trí cho phép cấu hình trong `get_ability_config` không:

```lua
-- Các mã vị trí mục tiêu được phép
"own_frontline"   -- Slot hàng tiền tuyến đồng minh
"own_backline"    -- Slot hàng hậu thuẫn đồng minh
"own_hand"        -- Slot bài trên tay đồng minh
"own_void"        -- Mộ bài đồng minh
"own_source"      -- Bộ bài rút đồng minh
"enemy_frontline" -- Slot hàng tiền tuyến đối thủ
"enemy_backline"  -- Slot hàng hậu thuẫn đối thủ
"enemy_hand"      -- Slot bài trên tay đối thủ
"enemy_void"      -- Mộ bài đối thủ
"enemy_source"    -- Bộ bài rút đối thủ
```

---

## 2. Danh Mục Kỹ Năng Theo Chủng Tộc

Danh mục bên dưới chỉ liệt kê tên và số sao của từng thẻ kỹ năng.

### 🛡️ Chủng Tộc Nhân Loại (Human)
- [Spinning Slash](cards/human/abilities/spinning_slash.md): 3 sao
- [Cross Guard](cards/human/abilities/cross_guard.md): 1 sao
- [Titan Fall](cards/human/abilities/titan_fall.md): 5 sao

### 🌿 Chủng Tộc Tinh Linh (Elf / Light Elf)
- [Holy Glow](cards/elf/abilities/holy_glow.md): 0 sao

### 👺 Chủng Tộc Yêu Tinh (Goblin)
- [Totem Pulse](cards/goblin/abilities/totem_pulse.md): 0 sao
- [Back Stab](cards/goblin/abilities/back_stab.md): 0 sao

### 💀 Chủng Tộc Bóng Tối (Dark Elf / Undead)
- [Twin Reaper](cards/dark_elf/abilities/twin_reaper.md): 0 sao
- [Skeleton Shield](cards/dark_elf/abilities/skeleton_shield.md): 0 sao
- [Animate Dead](cards/dark_elf/abilities/animate_dead.md): 0 sao
