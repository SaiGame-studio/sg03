# Eagle Eye

- **Loại Thẻ**: [`ability`](../../../04_abilities.md)
- **Số sao**: 2
- **Chủng Tộc**: `human` (Nhân Loại)
- **Vị Trí Nhắm Mục Tiêu**: Một lá bài đang úp của đối thủ
- **Thẻ Nhân Vật Yêu Cầu**: [Lyra](../lyra.md)

## Mô Tả Kỹ Năng

Lyra phái đại bàng chiến bay qua chiến tuyến của đối thủ. Từ trên cao, nó xác định chính xác thân phận của một lá bài đang úp và báo lại cho Lyra, biến yếu tố bất ngờ của đối thủ thành thông tin chiến thuật cho phe bạn.

## Điều Kiện Sử Dụng

Lyra phải đang có mặt trên sới đấu của phe bạn. Chọn 1 lá bài đang úp của đối thủ; mục tiêu có thể là thẻ Character hoặc Ability.

## Hiệu Quả

Lá bài được chọn bị **Expose**: đặt `face_up = true` và `expose = true`, để toàn bộ thông tin của lá bài hiển thị cho người chơi. Eagle Eye không gây sát thương, không thay đổi ATK/DEF và không kích hoạt hiệu ứng của lá bài bị lộ. Việc sử dụng Eagle Eye **không** đặt Lyra vào trạng thái `trigger`, vì vậy cô vẫn có thể thực hiện hành động khác trong lượt nếu các điều kiện khác cho phép.

Khi Lyra tấn công, nội tại **Scout Strike** cũng sử dụng hiệu ứng Eagle Eye lên 1 lá bài đang úp liền kề mục tiêu bị tấn công (bên trái hoặc bên phải, cùng hàng). Lần Expose phát sinh từ nội tại này không làm thay đổi trạng thái `trigger` của Lyra.

## Lý Do Chọn 2 Sao

Eagle Eye là kỹ năng trinh sát đơn chức năng, chỉ tác động lên một lá bài. Giá trị của nó nằm ở việc phá vỡ thông tin ẩn để hỗ trợ quyết định chiến thuật, phù hợp với thẻ Ability 2 sao trong giai đoạn early game.
