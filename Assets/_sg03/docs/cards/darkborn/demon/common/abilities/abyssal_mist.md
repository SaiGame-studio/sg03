# Abyssal Mist

- **Mã Thẻ Bài**: `abyssal_mist`
- **Số sao**: 3
- **Loại Thẻ**: [`ability`](../../../../../04_abilities.md)
- **Chủng Tộc Chính**: **Darkborn** (định danh kỹ thuật: `darkborn`)
- **Tộc Nhánh**: **Demon**
- **Thẻ Nhân Vật Yêu Cầu**: [Misthy](../misthy.md)
- **Điều Kiện Kích Hoạt**: Misthy đang trên sân và chưa kích hoạt (`trigger = false`).
- **Hiệu Ứng**:
  - Misthy nhận `atk_added = 150`.
  - Mọi Character Darkborn và Natureborn của cả Alpha lẫn Omega nhận `def_added = 100`.
- **Trạng Thái Sau Khi Kích Hoạt**: Misthy có `trigger = true`.
- **Thời Gian Tồn Tại**: Abyssal Mist ở lại trên sân cho đến khi bị một lá bài khác hủy.

---

## Mô Tả

Abyssal Mist được trích xuất từ ký ức mạnh mẽ của Misthy về quãng thời gian cô sống tại quê nhà dưới đáy vực, nơi quanh năm bị sương mù bao phủ. Khi được triệu hồi, màn sương bám trên chiến trường, khuếch đại linh lực của Misthy và bao phủ các Character Darkborn cùng Natureborn của cả hai phe.

Sau khi được triển khai, Abyssal Mist không tự biến mất. Nó duy trì trên sân cho đến khi một lá bài khác hủy hiệu ứng này.

## Các Bước Thực Thi

1. Kiểm tra Misthy đang trên sân và chưa kích hoạt.
2. Đặt `trigger = true` cho Misthy.
3. Đưa Abyssal Mist lên sân và áp dụng các hiệu ứng được khai báo trong metadata.
4. Giữ Abyssal Mist trên sân cho đến khi một lá bài khác hủy nó.
