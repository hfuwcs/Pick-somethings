# 🎯 Hướng dẫn Setup Góc Ban Đầu Cho Con Lắc

## 📋 Tổng quan
Tính năng mới cho phép người dùng **kéo con lắc đến góc mong muốn** trước khi bắt đầu thí nghiệm, thay vì phải bắt đầu từ vị trí thẳng đứng.

---

## 🔧 Cách sử dụng

### **Bước 1: Lắp ráp con lắc**
1. Cầm quả nặng (Pendulum Bob)
2. Đưa đến gần điểm treo (Pivot Point)
3. Nhấn phím tương tác để **Snap** vào điểm treo

➡️ **Kết quả:** Con lắc được lắp ráp, LineRenderer hiển thị sợi dây

---

### **Bước 2: Setup góc ban đầu (Setup Phase)**
Khi con lắc đã được lắp ráp và **chưa bấm Start**:

1. **Nhấn giữ phím cầm** (Select) vào quả nặng
2. **Kéo chuột** để điều chỉnh góc
3. Con lắc sẽ chỉ di chuyển trong phạm vi **±maxSetupAngleDegrees** (mặc định: ±15°)
4. Góc hiện tại sẽ được hiển thị trong Console
5. **Thả phím cầm** khi đã chọn được góc mong muốn

> ⚠️ **Lưu ý:** Giới hạn góc đảm bảo con lắc hoạt động trong chế độ dao động điều hòa (góc nhỏ)

---

### **Bước 3: Bắt đầu thí nghiệm**
1. Nhấn nút **"Start Experiment"** trong UI
2. Con lắc sẽ bắt đầu dao động từ **góc hiện tại**
3. Giới hạn góc sẽ được **tự động gỡ bỏ** khi thí nghiệm chạy

---

## ⚙️ Cấu hình

### **Trong Inspector của PendulumExperimentManager:**

| Tham số | Mô tả | Giá trị đề xuất |
|---------|-------|-----------------|
| `Max Setup Angle Degrees` | Góc tối đa có thể kéo trong setup phase | **15°** (đảm bảo dao động điều hòa) |

### **Tùy chỉnh nâng cao:**

Nếu muốn thay đổi giới hạn góc trong code:

```csharp
[Range(5f, 30f)]
[SerializeField] private float maxSetupAngleDegrees = 15f;
```

---

## 🎮 Luồng hoạt động

```
┌─────────────────────────────────────────────────────────────┐
│  1. Lắp ráp con lắc (Snap vào Pivot Point)                  │
│     ↓                                                        │
│  2. [Setup Phase] Kéo con lắc đến góc mong muốn (±15°)      │
│     ↓                                                        │
│  3. Nhấn "Start Experiment"                                  │
│     ↓                                                        │
│  4. [Running] Con lắc dao động từ góc đã chọn               │
│     ↓                                                        │
│  5. Nhấn "Reset" để quay về bước 1                          │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔍 Các trạng thái của hệ thống

### **PreExperiment (Trước khi Start)**
- ✅ Cho phép kéo con lắc trong giới hạn góc
- ✅ Áp dụng `PendulumHoldStrategy` với `enforceAngleLimit = true`
- ✅ Hiển thị góc hiện tại trong Console

### **Running (Sau khi Start)**
- ❌ Không cho phép kéo tự do
- ✅ Áp dụng `PendulumHoldStrategy` với `enforceAngleLimit = false`
- ✅ Con lắc dao động theo vật lý hoặc mô phỏng lý tưởng

---

## 🐛 Xử lý lỗi

### **Vấn đề: Không thể kéo con lắc**
**Nguyên nhân:** Thí nghiệm đã bắt đầu (state = Running)  
**Giải pháp:** Nhấn Reset trước khi kéo

### **Vấn đề: Con lắc bị giới hạn góc khi đang chạy**
**Nguyên nhân:** Lỗi logic  
**Kiểm tra:** `_isInSetupPhase` phải là `false` khi Running

---

## 📊 Chi tiết kỹ thuật

### **PendulumHoldStrategy**
```csharp
public PendulumHoldStrategy(
    Transform pivot, 
    float length, 
    float maxAngleDegrees = 90f,   // Góc tối đa
    bool enforceAngleLimit = false  // Có áp dụng giới hạn?
)
```

### **Flow trong CheckAssemblyState()**
```csharp
// Khi lắp ráp
bool enforceLimit = (CurrentState == ExperimentState.PreExperiment);
var strategy = new PendulumHoldStrategy(
    pivotPoint.transform, 
    length, 
    maxSetupAngleDegrees, 
    enforceLimit  // true nếu đang ở PreExperiment
);
```

---

## ✅ Checklist test

- [ ] Lắp ráp con lắc thành công
- [ ] Kéo con lắc trong setup phase (PreExperiment)
- [ ] Góc bị giới hạn đúng ±15° (hoặc giá trị custom)
- [ ] Nhấn Start, con lắc dao động từ góc đã chọn
- [ ] Giới hạn góc tự động gỡ bỏ khi Running
- [ ] Reset trả về trạng thái ban đầu
- [ ] Unsnap và snap lại vẫn hoạt động bình thường

---

## 🎓 Ý nghĩa vật lý

Giới hạn góc **±15°** (≈ 0.26 rad) đảm bảo:
- `sin(θ) ≈ θ` (sai số < 1%)
- Con lắc dao động điều hòa đơn
- Chu kỳ không phụ thuộc vào biên độ (T = 2π√(L/g))

Nếu góc lớn hơn 15°, công thức chu kỳ cần điều chỉnh:
```
T ≈ 2π√(L/g) × [1 + (1/16)θ₀² + ...]
```

---

**Tài liệu này được tạo tự động bởi GitHub Copilot** 🤖
