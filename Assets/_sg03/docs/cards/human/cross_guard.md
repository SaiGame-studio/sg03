# Cross Guard (Đỡ Kiếm Phản Vệ)

- **Mã Thẻ Bài**: `cross_guard`
- **Loại Thẻ**: `ability`
- **Chủng Tộc**: `human` (Nhân Loại)
- **Vị Trí Nhắm Mục Tiêu**: `own_frontline`
- **Thẻ Nhân Vật Yêu Cầu**: [Azure Blade](azure_blade.md)

---

## 🛡️ Cơ Chế & Luồng Thực Thi Kỹ Năng

- **Cấu Hình**: `target_positions = { "own_frontline" }`, `requires_target_card = true`
- **Điều Kiện**: Yêu cầu 1 thẻ nhân vật [Azure Blade](azure_blade.md) chưa kích hoạt trên `own_frontline`.

### Các Bước Thực Thi
1. Tìm thẻ `azure_blade` chưa kích hoạt trên `own_frontline`.
2. Đánh dấu `azure_blade.trigger = true` và lật ngửa bài.
3. Cộng trực tiếp $+200$ điểm Giáp/Phòng thủ cho thẻ bài phòng thủ mục tiêu đồng minh:
   $$\text{final\_def} = \text{final\_def} + 200$$
4. Phát chuỗi Client Action: `[side]_card_expose`, `[side]_card_ability`, `[side]_card_guarded:[target_id]`.
