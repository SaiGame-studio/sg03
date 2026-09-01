# King Return

- **Mã Thẻ Bài**: `king_return`
- **Số sao**: 4
- **Loại Thẻ**: [`ability`](../../../../../04_abilities.md)
- **Chủng Tộc Chính**: **Darkborn**
- **Tộc Nhánh**: Undead (định danh kỹ thuật: `darkborn` / `undead`)
- **Vị Trí Nhắm Mục Tiêu**: `own_frontline`
- **Thẻ Nhân Vật Yêu Cầu**: [Ria](../ria.md) và 3 [Skeleton](../skeleton.md)
- **Thẻ Nhân Vật Được Triệu Hồi**: [Skeleton King](../skeleton_king.md)

---

## Mô Tả Kỹ Năng

Ria gọi Skeleton King trở lại chiến trường bằng cách hiến tế ba Skeleton đang ở hàng trước. King Return tự chọn vị trí trống liền kề bên trái Ria trước, rồi mới chọn bên phải, để Skeleton King xuất hiện.

## Điều Kiện Sử Dụng

- Có một [Ria](../ria.md) trên `own_frontline`.
- Có ít nhất ba [Skeleton](../skeleton.md) trên `own_frontline` để hiến tế.
- Có [Skeleton King](../skeleton_king.md) trong `the_void`.

## Cơ Chế & Luồng Thực Thi

1. Chọn Ria trên `own_frontline` làm nhân vật thực hiện kỹ năng.
2. Đưa ba Skeleton đầu tiên theo thứ tự slot từ `own_frontline` vào `the_void`.
3. Nếu còn vị trí trống liền kề bên trái hoặc bên phải Ria, King Return tự chọn ô bên trái trước, rồi đến ô bên phải, để đưa Skeleton King từ `the_void` vào `own_frontline`.
4. Nếu sau khi hiến tế ba Skeleton không có vị trí trống liền kề Ria, đưa `king_return` vào `the_void`; không triệu hồi Skeleton King và không tạo thêm hiệu ứng nào khác.
