# Xena Awakened I

- **Mã Thẻ Bài**: `xena_awakened1`
- **Số sao**: 1
- **Loại Thẻ**: [`ability`](../../../../04_abilities.md)
- **Chủng Tộc Chính**: **Darkborn** (định danh kỹ thuật: `darkborn`)
- **Thẻ Nhân Vật Yêu Cầu**: [Xena I](../xena1.md)

---

## Cơ Chế Kỹ Năng

Khi [Xena I](../xena1.md) bị hạ gục sau khi đòn tấn công của đối thủ đang nhắm vào cô được giải quyết, có thể kích hoạt **Xena Awakened I**.

Triệu hồi 1 [Xena II](../xena2.md) **chỉ từ `the_void`** của phe bạn vào sân. Xena II được triệu hồi nhận **+100 DEF**.

### Điều Kiện

1. Xena I phải là mục tiêu của đòn tấn công đối phương và chắc chắn sẽ bị hạ gục bởi đòn tấn công đó.
2. Có ít nhất 1 Xena II trong `the_void`.
3. Không yêu cầu Xena I phải chưa kích hoạt; kỹ năng vẫn dùng được dù `trigger` của Xena I đang là `true`.

> **Lưu ý:** Nếu kích hoạt khi Xena I vẫn trụ được sau đòn tấn công, không có hiệu ứng nào xảy ra và `xena_awakened1` vẫn được đưa vào `the_void`.

### Kết Quả

1. Xena I bị hạ gục được chuyển vào `the_void`.
2. Chọn 1 Xena II từ `the_void` và triệu hồi vào đúng vị trí trên sân mà Xena I vừa rời khỏi.
3. Tăng DEF hiện có của Xena II được triệu hồi thêm `+100`.

## Kỹ Năng Liên Kết

- [Xena Awakened II](xena_awakened2.md) — 2 sao
- [Xena Awakened III](xena_awakened3.md) — 3 sao
- [Xena Awakened IV](xena_awakened4.md) — 4 sao
