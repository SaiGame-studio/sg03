# Spinning Slash (Trảm Xoay)

- **Mã Thẻ Bài**: `spinning_slash`
- **Loại Thẻ**: `ability`
- **Chủng Tộc**: `human` (Nhân Loại)
- **Vị Trí Nhắm Mục Tiêu**: `enemy_frontline`
- **Thẻ Nhân Vật Yêu Cầu**: [Azure Blade](azure_blade.md)

---

## ⚔️ Cơ Chế & Luồng Thực Thi Kỹ Năng

- **Cấu Hình**: `target_positions = { "enemy_frontline" }`, `is_character_ability = true`, `requires_target_card = true`
- **Điều Kiện**: Yêu cầu 1 thẻ nhân vật [Azure Blade](azure_blade.md) chưa kích hoạt trên `own_frontline`.

### Các Bước Thực Thi
1. Tìm thẻ `azure_blade` chưa kích hoạt trên `own_frontline`. Báo lỗi nếu không có.
2. Đánh dấu `azure_blade.trigger = true` và lật ngửa bài (`expose_ability_selected_card`).
3. Đọc chỉ số `atk_add` từ định nghĩa `spinning_slash` và `char_atk` từ `azure_blade`.
4. Tính tổng sát thương:
   $$\text{Tổng Sát Thương} = \text{atk\_add} + \text{char\_atk}$$
5. Gây sát thương lên thẻ bài phòng thủ mục tiêu phía đối thủ.
6. Phát chuỗi Client Action: `[side]_card_expose`, `[side]_card_ability`, `[side]_card_take_damage`.
