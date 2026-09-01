# Skeleton Shield

- **Mã Thẻ Bài**: `skeleton_shield`
- **Loại Thẻ**: [`ability`](../../../../../04_abilities.md)
- **Chủng Tộc Chính**: **Darkborn**
- **Tộc Nhánh**: Undead (định danh kỹ thuật: `darkborn` / `undead`)
- **Vị Trí Nhắm Mục Tiêu**: `own_frontline`
- **Thẻ Nhân Vật Yêu Cầu**: [Ria](../ria.md) & [Skeleton](../skeleton.md)

---

## ☠️ Cơ Chế & Luồng Thực Thi Kỹ Năng

- **Cấu Hình**: `target_positions = { "own_frontline" }`, `requires_target_card = true`
- **Điều Kiện**: Yêu cầu 1 thẻ [Ria](../ria.md) trên `own_frontline` VÀ 1 thẻ [Skeleton](../skeleton.md) chưa kích hoạt khác trên `own_frontline`. Thẻ mục tiêu phải đang bị đối phương chọn làm mục tiêu tấn công.

### Các Bước Thực Thi
1. Tìm `ria` và 1 `skeleton` chưa kích hoạt (khác mục tiêu).
2. Xác minh danh sách kế hoạch tấn công của đối thủ (`opponent_planning`) có chứa đòn tấn công nhắm vào `target_card`.
3. Tráo đổi chỉ số slot mảng (`slot_index`) giữa `skeleton_card` và `target_card`.
4. Chuyển hướng đòn tấn công của đối thủ (`defender_inv_id`) sang cho `skeleton_card` gánh chịu.
5. Đánh dấu `ria.trigger = true` và lật ngửa bài.
6. Phát Client Action `[side]_card_swapped:card1=[skel_id],card2=[target_id]` và `[side]_card_guarded:[target_id]`.
