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

## 2. Danh Mục & Liên Kết Chi Tiết Kỹ Năng Theo Chủng Tộc

Chi tiết luồng thực thi, các bước xử lý và hiệu ứng của từng kỹ năng được biên soạn cụ thể trong tệp tài liệu của từng Thẻ Kỹ Năng tương ứng bên dưới:

### 🛡️ Chủng Tộc Nhân Loại (Human)
- [Spinning Slash (Trảm Xoay)](cards/human/spinning_slash.md) — Chiêu thức chém xoay gây sát thương tổng nhắm vào tiền tuyến đối thủ.
- [Cross Guard (Đỡ Kiếm Phản Vệ)](cards/human/cross_guard.md) — Chiêu thức đỡ kiếm tăng +200 giáp cho đơn vị phòng thủ đồng minh.

### 🌿 Chủng Tộc Tinh Linh (Elf / Light Elf)
- [Holy Glow (Thánh Quang Hồi Năng)](cards/elf/holy_glow.md) — Phép thuật hồi phục HP cho Player và tiêu thụ lá bài vào mộ.

### 👺 Chủng Tộc Yêu Tinh (Goblin)
- [Totem Pulse (Sóng Mạch Totem)](cards/goblin/totem_pulse.md) — Sóng Totem tăng giáp phòng thủ cho toàn bộ sới tiền tuyến đồng minh.
- [Back Stab (Đánh Lén Đao Độc)](cards/goblin/back_stab.md) — Chiêu thức đao độc đánh lén gây sát thương lớn lên mục tiêu đối thủ.

### 💀 Chủng Tộc Bóng Tối (Dark Elf / Undead)
- [Twin Reaper (Song Tử Trảm)](cards/dark_elf/twin_reaper.md) — Trảm song đao gây sát thương chém lan sang đơn vị kề cận.
- [Skeleton Shield (Lá Chắn Xương)](cards/dark_elf/skeleton_shield.md) — Tráo đổi lính xương gánh đòn tấn công thay cho mục tiêu đồng minh.
- [Animate Dead (Chiêu Hồn Binh Đoàn Xương)](cards/dark_elf/animate_dead.md) — Hồi sinh tối đa 3 lính xương từ mộ `the_void` lên tiền tuyến.
