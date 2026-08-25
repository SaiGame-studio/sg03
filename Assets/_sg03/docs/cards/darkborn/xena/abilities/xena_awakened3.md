# Xena Awakened III

- **Mã Thẻ Bài**: `xena_awakened3`
- **Số sao**: 3
- **Loại Thẻ**: [`ability`](../../../../04_abilities.md)
- **Chủng Tộc Chính**: **Darkborn** (định danh kỹ thuật: `darkborn`)
- **Thẻ Nhân Vật Yêu Cầu**: [Xena III](../xena3.md)

---

## Cơ Chế Kỹ Năng

Khi [Xena III](../xena3.md) chắc chắn sẽ bị hạ gục sau khi đòn tấn công của đối thủ đang nhắm vào cô được tung ra, có thể kích hoạt **Xena Awakened III**.

Hiến tế 1 thẻ bài đồng minh thuộc chủng tộc `darkborn` từ 1 đến 3 sao, nằm liền kề bên trái hoặc bên phải của Xena III, vào `the_void`, sau đó triệu hồi 1 [Xena IV](../xena4.md) **chỉ từ `the_void`** của phe bạn vào đúng vị trí trên sân mà Xena III vừa rời khỏi. Xena IV được triệu hồi **không nhận buff DEF**.

### Điều Kiện

1. Xena III phải là mục tiêu của đòn tấn công đối phương và chắc chắn sẽ bị hạ gục bởi đòn tấn công đó.
2. Có ít nhất 1 Xena IV trong `the_void`.
3. Có ít nhất 1 thẻ bài đồng minh thuộc chủng tộc `darkborn` từ 1 đến 3 sao nằm ở vị trí liền kề bên trái hoặc bên phải của Xena III để hiến tế.
4. Không yêu cầu Xena III phải chưa kích hoạt; kỹ năng vẫn dùng được dù `trigger` của Xena III đang là `true`.
5. Không thể hiến tế bất kỳ lá bài Xena nào.

> **Lưu ý:** Nếu kích hoạt khi Xena III vẫn trụ được sau đòn tấn công, không có hiệu ứng nào xảy ra và `xena_awakened3` vẫn được đưa vào `the_void`.

### Kết Quả

1. Xena III bị hạ gục được chuyển vào `the_void`.
2. Chọn 1 thẻ bài đồng minh thuộc chủng tộc `darkborn` từ 1 đến 3 sao ở vị trí liền kề bên trái hoặc bên phải của Xena III và hiến tế thẻ đó vào `the_void`. Nếu có nhiều thẻ hợp lệ, ưu tiên chọn thẻ có số sao thấp nhất.
3. Chọn 1 Xena IV từ `the_void` và triệu hồi vào đúng vị trí trên sân mà Xena III vừa rời khỏi.
4. Xena IV được triệu hồi giữ nguyên chỉ số DEF hiện có, không nhận buff DEF.

## Kỹ Năng Liên Kết

- [Xena Awakened I](xena_awakened1.md) — 1 sao
- [Xena Awakened II](xena_awakened2.md) — 2 sao
- [Xena Awakened IV](xena_awakened4.md) — 4 sao
