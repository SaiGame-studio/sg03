# Twin Reaper (Song Tử Trảm)

- **Mã Thẻ Bài**: `twin_reaper`
- **Loại Thẻ**: `ability`
- **Chủng Tộc**: `dark_elf` / `undead` (Bóng Tối / Tử Thần)
- **Vị Trí Nhắm Mục Tiêu**: `enemy_frontline`
- **Thẻ Nhân Vật Yêu Cầu**: [Hellscythe](hellscythe.md)

---

## ☠️ Cơ Chế & Luồng Thực Thi Kỹ Năng

- **Cấu Hình**: `target_positions = { "enemy_frontline" }`
- **Sự Kiện Kích Hoạt**: Giao chiến tấn công (`on_attack`).

### Các Bước Thực Thi
1. Xác định thẻ bài phòng thủ chính từ `event_data`.
2. Quét hàng phòng thủ tìm đơn vị nằm ở slot kề cận $\text{defender\_slot} + 1$ hoặc $\text{defender\_slot} - 1$.
3. Nếu có đơn vị kề cận, tính sát thương bằng chỉ số Tấn công cơ bản của bên công (`base_stats.atk`).
4. Gây sát thương lên đơn vị kề cận qua `deal_damage_to_character`.
5. Phát Client Action `[side]_card_ability:source=[src],ability=twin_reaper,target=[adjacent_id]`.
