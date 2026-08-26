# Totem Pulse

- **Mã Thẻ Bài**: `totem_pulse`
- **Loại Thẻ**: [`ability`](../../../../04_abilities.md)
- **Chủng Tộc Chính**: **Natureborn**
- **Tộc Nhánh**: Goblin (định danh kỹ thuật: `goblin`)
- **Vị Trí Nhắm Mục Tiêu**: `own_frontline`
- **Thẻ Nhân Vật Yêu Cầu**: [Goblin Shaman](../goblin_shaman.md)

## Mô Tả Kỹ Năng

Goblin Shaman dựng Totem giữa chiến địa và dẫn truyền vào đó linh lực nguyên thủy của đất, cây cỏ cùng sức sống hoang dã. Khi nghi lễ hoàn tất, Totem cộng hưởng với nhịp sống của vùng đất rồi giải phóng một xung năng lượng lan dọc toàn bộ tiền tuyến phe mình.

Luồng linh lực bao bọc các đồng minh như một lớp vỏ tự nhiên, củng cố khả năng phòng thủ và giúp đội hình đứng vững trước đòn tấn công đang ập đến. Sau khi truyền hết năng lượng bảo hộ, Totem hoàn thành sứ mệnh và được đưa vào `the_void`.

---

## 🗿 Cơ Chế & Luồng Thực Thi Kỹ Năng

- **Cấu Hình**: `target_positions = { "own_frontline" }`
- **Điều Kiện**: Yêu cầu 1 thẻ nhân vật [Goblin Shaman](../goblin_shaman.md) chưa kích hoạt trên `own_frontline`.

### Các Bước Thực Thi
1. Tìm `goblin_shaman` chưa kích hoạt trên `own_frontline`.
2. Đánh dấu `shaman.trigger = true` và lật ngửa bài.
3. Đọc chỉ số `def_add` từ định nghĩa lá bài Totem.
4. Lặp qua **tất cả thẻ bài** trên `own_frontline` và cộng `def_add` vào `final_def` của từng đơn vị.
5. Tiêu thụ lá bài Totem: loại khỏi `back_line` và chuyển vào mộ `the_void`.
6. Phát Client Action `[side]_card_ability` kèm hiệu ứng buff giáp và `[side]_card_sent_to_void:[totem_id]`.
