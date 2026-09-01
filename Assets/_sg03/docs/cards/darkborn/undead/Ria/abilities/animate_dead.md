# Animate Dead

- **Mã Thẻ Bài**: `animate_dead`
- **Số sao**: 3
- **Loại Thẻ**: [`ability`](../../../../../04_abilities.md)
- **Chủng Tộc Chính**: **Darkborn**
- **Tộc Nhánh**: Undead (định danh kỹ thuật: `darkborn` / `undead`)
- **Vị Trí Nhắm Mục Tiêu**: `own_frontline` / `own_hand`
- **Thẻ Nhân Vật Yêu Cầu**: [Ria](../ria.md)

---

## ☠️ Cơ Chế & Luồng Thực Thi Kỹ Năng

- **Cấu Hình**: `target_positions = { "own_frontline", "own_backline", "own_source", "own_void" }`
- **Điều Kiện**: Yêu cầu 1 thẻ [Ria](../ria.md) ở `own_frontline`.
- **Sát thương lên Ria**: Sau khi resolve, Ria nhận lượng sát thương bằng `stats.atk` của Animate Dead.

### Các Bước Thực Thi
1. Tìm `ria` ở `own_frontline` làm nhân vật thực hiện kỹ năng.
2. Triệu gọi nhiều lá [Skeleton](../skeleton.md) từ `the_void`.
3. Với mỗi lá skeleton tìm thấy trong `the_void`:
   - Tìm slot trống khả dụng (1 đến 5) trên `own_frontline`.
   - Loại bỏ skeleton khỏi `the_void`.
   - Reset chỉ số bài (`reset_card_turn_state`), gán `trigger = false`, `face_up = true`, `expose = true`.
   - Đặt skeleton vào slot trống đã chọn trên `own_frontline`.
   - Phát Client Action `[side]_void_to_front_line:[skeleton_id],[slot_index]`.
4. Ria nhận sát thương bằng `stats.atk` của Animate Dead.
5. Tiêu thụ lá bài kỹ năng `animate_dead` và chuyển vào `the_void`.
