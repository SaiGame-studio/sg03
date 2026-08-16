# Animate Dead (Chiêu Hồn Binh Đoàn Xương)

- **Mã Thẻ Bài**: `animate_dead`
- **Loại Thẻ**: [`ability`](../../../04_abilities.md)
- **Chủng Tộc**: `dark_elf` / `undead` (Bóng Tối / Tử Thần)
- **Vị Trí Nhắm Mục Tiêu**: `own_frontline` / `own_hand`
- **Thẻ Nhân Vật Yêu Cầu**: [Hellscythe](../hellscythe.md)

---

## ☠️ Cơ Chế & Luồng Thực Thi Kỹ Năng

- **Cấu Hình**: `target_positions = { "own_frontline", "own_backline", "own_source", "own_void" }`
- **Điều Kiện**: Yêu cầu 1 thẻ [Hellscythe](../hellscythe.md) chưa kích hoạt ở `own_frontline`.

### Các Bước Thực Thi
1. Tìm `hellscythe` chưa kích hoạt ở `own_frontline`. Đánh dấu `hellscythe.trigger = true`.
2. Lặp tối đa 3 lần để hồi sinh tối đa 3 lá [Skeleton](../skeleton.md) từ mộ `the_void`.
3. Với mỗi lá skeleton tìm thấy trong `the_void`:
   - Tìm slot trống khả dụng (1 đến 5) trên `own_frontline`.
   - Loại bỏ skeleton khỏi `the_void`.
   - Reset chỉ số bài (`reset_card_turn_state`), gán `trigger = true`, `face_up = true`, `expose = true`.
   - Đặt skeleton vào slot trống đã chọn trên `own_frontline`.
   - Phát Client Action `[side]_void_to_front_line:[skeleton_id],[slot_index]`.
4. Tiêu thụ lá bài kỹ năng `animate_dead` và chuyển vào `the_void`.
