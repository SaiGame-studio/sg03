# Back Stab

- **Mã Thẻ Bài**: `back_stab`
- **Loại Thẻ**: [`ability`](../../../../04_abilities.md)
- **Số sao**: 2
- **Chủng Tộc Chính**: **Natureborn**
- **Tộc Nhánh**: Goblin (định danh kỹ thuật: `goblin`)
- **Vị Trí Nhắm Mục Tiêu**: `enemy_frontline`
- **Thẻ Nhân Vật Yêu Cầu**: [Goblin Grunt](../goblin_grunt.md)

## Mô Tả Kỹ Năng

Back Stab là chiến thuật đánh úp đặc trưng của Goblin Grunt. Họ tận dụng bụi rậm, địa hình gồ ghề cùng sự hỗn loạn trên chiến trường để lẩn khỏi tầm quan sát, vòng qua phòng tuyến rồi bất ngờ tấn công vào điểm mù của đối phương.

Khi kích hoạt, sức tấn công của Goblin Grunt được kết hợp với sức mạnh của Back Stab để tạo thành một đòn sát thương tập trung. Không Character Goblin nào khác có thể sử dụng kỹ năng này.

---

## 🗡️ Cơ Chế & Luồng Thực Thi Kỹ Năng

- **Cấu Hình**: `target_positions = { "enemy_frontline" }`, `requires_target_card = true`
- **Điều Kiện**: Yêu cầu một `goblin_grunt` chưa kích hoạt trên `own_frontline`.

### Các Bước Thực Thi
1. Tìm `goblin_grunt` chưa kích hoạt trên `own_frontline`.
2. Đảm bảo mục tiêu phòng thủ đối thủ không phải chính `goblin_grunt` đó.
3. Đánh dấu `goblin_grunt.trigger = true` và lật ngửa bài.
4. Tính tổng sát thương gây ra:
   $$\text{Tổng Sát Thương} = \text{atk\_add} + \text{ATK}_{goblin\_grunt}$$
5. Gây tổng sát thương lên mục tiêu phòng thủ đối thủ.
6. Phát Client Action `[side]_card_expose`, `[side]_card_ability`, `[side]_card_take_damage`.
