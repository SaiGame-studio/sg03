# Xena Awakened IV

- **Mã Thẻ Bài**: `xena_awakened4`
- **Số sao**: 4
- **Loại Thẻ**: [`ability`](../../../../04_abilities.md)
- **Chủng Tộc**: `dark_elf` (Dark Elf)
- **Thẻ Nhân Vật Yêu Cầu**: [Xena 4 sao](../xena4.md)
- **Bài Yêu Cầu**: [Demon Orbs](demon_orbs.md) và [Demon Rite](demon_rite.md)

---

## Cơ Chế Kỹ Năng

Khi [Xena 4 sao](../xena4.md) bị hạ gục sau khi đòn tấn công của đối thủ đang nhắm vào cô được triểu khai, có thể kích hoạt **Xena Awakened IV**. Nếu toàn bộ điều kiện nghi thức được đáp ứng, Xena được hồi sinh thành [Xena 5 sao](../xena5.md) bằng nghi thức của Quỷ Vương.

### Điều Kiện Kích Hoạt Thành Công

1. Xena 4 sao phải là mục tiêu của đòn tấn công đối phương và chắc chắn sẽ bị hạ gục bởi đòn tấn công đó.
2. Có ít nhất 1 Xena 5 sao trong `the_void`.
3. [Demon Orbs](demon_orbs.md) và [Demon Rite](demon_rite.md) đều đã được triển khai trên `own_backline`.
4. Điều kiện hiến tế của Demon Rite được thỏa mãn: có ít nhất 1 thẻ bài đồng minh từ 1 đến 4 sao đứng liền kề bên trái hoặc bên phải của Xena 4 sao. Nếu có nhiều thẻ hợp lệ, ưu tiên thẻ có số sao thấp nhất.

### Kết Quả Khi Thành Công

1. Xena 4 sao bị hạ gục được chuyển vào `the_void`.
2. Hiến tế thẻ bài hợp lệ theo điều kiện của Demon Rite và đưa thẻ đó vào `the_void`.
3. Triệu hồi 1 Xena 5 sao từ `the_void` vào đúng vị trí trên sân mà Xena 4 sao vừa rời khỏi.

### Kết Quả Khi Thất Bại

Nếu Demon Orbs, Demon Rite hoặc điều kiện hiến tế không thỏa mãn, `xena_awakened4` được xem là kích hoạt thất bại và được đưa vào `the_void`; không có hiệu ứng hồi sinh hoặc triệu hồi nào xảy ra.

## Bài Liên Kết

- [Demon Orbs](demon_orbs.md) — 3 sao
- [Demon Rite](demon_rite.md) — 3 sao

## Kỹ Năng Liên Kết

- [Xena Awakened I](xena_awakened1.md) — 1 sao
- [Xena Awakened II](xena_awakened2.md) — 2 sao
- [Xena Awakened III](xena_awakened3.md) — 3 sao
