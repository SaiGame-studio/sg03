# Spear Sweep (Quét Giáo)

- **Loại Thẻ**: [`ability`](../../../04_abilities.md)
- **Mã thẻ bài**: `titan_spear_sweep`
- **Số sao**: 2
- **Chủng tộc**: `human` (Nhân Loại)
- **Vị trí nhắm mục tiêu**: Toàn bộ chiến trường
- **Thẻ Nhân Vật Yêu Cầu**: [Titan](../titan.md)

## Mô Tả Kỹ Năng

Titan xoay ngọn giáo khổng lồ của mình, tạo ra một nhát quét năng lượng quét qua chiến tuyến đối thủ. Lực quét mạnh đến mức có thể vạ lây một đồng minh đứng sát bên Titan.

## Điều Kiện Sử Dụng

Titan phải đang có mặt trên chiến trường của phe bạn và chưa hành động trong lượt.

## Hiệu Quả

Khi kích hoạt, Titan gây **160 damage** cho toàn bộ thẻ `character` ở hàng trước và hàng sau của phe đối thủ. Titan cũng gây **160 damage** cho một Character đồng minh kề bên trên cùng hàng: ưu tiên ô bên phải, sau đó là ô bên trái. Nếu Character ở bên phải là Ren, Titan bỏ qua Ren và tìm Character ở bên trái.

Ren (`azure_blade`) không nhận damage. Nếu cả hai Character kề Titan đều là Ren, Titan bỏ qua damage đồng minh. Các thẻ không phải `character` không bị ảnh hưởng.

Sau khi sử dụng, Titan được tính là đã hành động trong lượt đó và thẻ Ability được đưa vào `the_void` của phe sử dụng.
