# Back Stab

- **Mã Thẻ Bài**: `back_stab`
- **Loại Thẻ**: [`ability`](../../../../04_abilities.md)
- **Chủng Tộc Chính**: **Natureborn**
- **Tộc Nhánh**: Goblin (định danh kỹ thuật: `goblin`)
- **Vị Trí Nhắm Mục Tiêu**: `enemy_frontline`
- **Thẻ Nhân Vật Yêu Cầu**: [Goblin Grunt](../goblin_grunt.md), [Goblin Saboteur](../goblin_saboteur.md) hoặc [Goblin Shaman](../goblin_shaman.md)
- **Ngoại Lệ**: [Goblin Brute](../goblin_brute.md) không thể sử dụng Back Stab

---

## 🗡️ Cơ Chế & Luồng Thực Thi Kỹ Năng

- **Cấu Hình**: `target_positions = { "enemy_frontline" }`, `requires_target_card = true`
- **Điều Kiện**: Yêu cầu 1 thẻ nhân vật thuộc tộc `goblin`, có code name khác `goblin_brute`, chưa kích hoạt trên `own_frontline`.

### Các Bước Thực Thi
1. Tìm đơn vị Goblin chưa kích hoạt trên `own_frontline`, đồng thời loại trừ `goblin_brute`.
2. Đảm bảo mục tiêu phòng thủ đối thủ không phải là chính đơn vị Goblin đó.
3. Đánh dấu `goblin.trigger = true` và lật ngửa bài.
4. Tính tổng sát thương gây ra:
   $$\text{Tổng Sát Thương} = \text{atk\_add} + \text{ATK}_{goblin}$$
5. Gây tổng sát thương lên mục tiêu phòng thủ đối thủ.
6. Phát Client Action `[side]_card_expose`, `[side]_card_ability`, `[side]_card_take_damage`.
