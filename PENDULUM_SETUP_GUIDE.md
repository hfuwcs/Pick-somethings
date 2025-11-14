# 🎯 Hướng dẫn Setup Góc Ban Đầu Cho Con Lắc

## 📋 Tổng quan
Tính năng mới cho phép người dùng **kéo con lắc đến góc mong muốn** trước khi bắt đầu thí nghiệm.

> ⚠️ **Lưu ý quan trọng:** Tính năng setup góc ban đầu **CHỈ hoạt động với Ideal Mode**. Realistic Mode không có giai đoạn setup, con lắc sẽ dao động theo vật lý thực ngay khi bấm Start.

---

## 🎮 Hai chế độ thí nghiệm

### **1. Ideal Mode (Dao động điều hòa lý tưởng)** ✨
- ✅ **Có Setup Phase**: Người dùng kéo con lắc đến góc mong muốn
- ✅ **Con lắc đứng yên**: Khi thả ra, con lắc sẽ giữ nguyên vị trí (kinematic)
- ✅ **Bấm Start**: `IdealPendulumSimulator` bắt đầu mô phỏng từ góc đó
- ✅ **Giới hạn góc**: ±15° (đảm bảo dao động điều hòa)

### **2. Realistic Mode (Vật lý thực tế)** 🌍
- ❌ **Không có Setup Phase**: Không thể kéo để setup góc
- ❌ **Dao động tự do**: Con lắc tuân theo vật lý Unity khi bấm Start
- ✅ **Damping**: Có ma sát/lực cản (configurable)

---

## 🔧 Cách sử dụng (Ideal Mode)

### **Bước 1: Chọn Simulation Mode**
1. Chọn `PendulumExperimentManager` trong Inspector
2. Đặt **Experiment Mode** = `Ideal`
3. Điều chỉnh `Max Setup Angle Degrees` nếu cần (mặc định: 15°)

### **Bước 2: Lắp ráp con lắc**
1. Cầm quả nặng (Pendulum Bob)
2. Đưa đến gần điểm treo (Pivot Point)
3. Nhấn phím tương tác để **Snap** vào điểm treo

➡️ **Kết quả:** Con lắc được lắp ráp, LineRenderer hiển thị sợi dây, và hệ thống vào **Setup Phase**

---

### **Bước 3: Setup góc ban đầu (Setup Phase)** 🎯
Khi con lắc đã được lắp ráp và **chưa bấm Start** (chỉ Ideal Mode):

1. **Nhấn giữ phím cầm** (Select) vào quả nặng
2. **Kéo chuột** để điều chỉnh góc
3. Con lắc sẽ chỉ di chuyển trong phạm vi **±15°**
4. Góc hiện tại sẽ được hiển thị trong Console
5. **Thả phím cầm** → Con lắc **đứng yên** tại góc đã chọn (kinematic)

> 💡 **Điểm đặc biệt:** Con lắc sẽ GIỮ NGUYÊN vị trí khi thả ra, không dao động, không rơi xuống. Nó đợi bạn bấm Start!

---

### **Bước 4: Bắt đầu thí nghiệm**
1. Nhấn nút **"Start Experiment"** trong UI
2. `IdealPendulumSimulator` được kích hoạt
3. Con lắc bắt đầu dao động **từ góc hiện tại** với vận tốc ban đầu = 0
4. Giới hạn góc được gỡ bỏ (script điều khiển toàn bộ)

---

## 🔄 So sánh 2 chế độ

| Tính năng | Ideal Mode | Realistic Mode |
|-----------|------------|----------------|
| **Setup Phase** | ✅ Có | ❌ Không |
| **Kéo để chọn góc** | ✅ Có (±15°) | ❌ Không thể |
| **Con lắc đứng yên khi setup** | ✅ Có (kinematic) | ❌ N/A |
| **Điều khiển** | Script (IdealPendulumSimulator) | Physics Engine |
| **Damping** | ❌ Không (lý tưởng) | ✅ Có (configurable) |
| **Chu kỳ** | Tính theo công thức T=2π√(L/g) | Đo thực tế |

---

## ⚙️ Cấu hình

### **Trong Inspector của PendulumExperimentManager:**

| Tham số | Mô tả | Giá trị đề xuất |
|---------|-------|-----------------|
| `Experiment Mode` | Chế độ thí nghiệm | **Ideal** (để dùng setup) |
| `Max Setup Angle Degrees` | Góc tối đa setup (Ideal Mode) | **15°** |
| `Damping Factor` | Hệ số tắt dần (Realistic Mode) | **0.1** |

---

## 🎮 Luồng hoạt động chi tiết

### **Ideal Mode:**
```
┌─────────────────────────────────────────────────────────────┐
│  1. Chọn Mode = Ideal                                        │
│     ↓                                                        │
│  2. Lắp ráp con lắc (Snap)                                   │
│     ↓                                                        │
│  3. [Setup Phase - PreExperiment]                            │
│     • isKinematic = true (con lắc đứng yên)                  │
│     • Kéo con lắc đến góc mong muốn (±15°)                   │
│     • Thả ra → con lắc giữ nguyên vị trí                     │
│     ↓                                                        │
│  4. Nhấn "Start Experiment"                                  │
│     ↓                                                        │
│  5. [Running]                                                │
│     • IdealPendulumSimulator được kích hoạt                  │
│     • Đọc góc hiện tại làm góc ban đầu                       │
│     • Bắt đầu mô phỏng dao động điều hòa từ góc đó           │
│     ↓                                                        │
│  6. Nhấn "End" hoặc "Reset"                                  │
└─────────────────────────────────────────────────────────────┘
```

### **Realistic Mode:**
```
┌─────────────────────────────────────────────────────────────┐
│  1. Chọn Mode = Realistic                                    │
│     ↓                                                        │
│  2. Lắp ráp con lắc (Snap)                                   │
│     • isKinematic = false (vật lý thông thường)              │
│     • KHÔNG có Setup Phase                                   │
│     ↓                                                        │
│  3. Nhấn "Start Experiment"                                  │
│     ↓                                                        │
│  4. [Running]                                                │
│     • Physics engine điều khiển                              │
│     • Áp dụng damping factor                                 │
│     • Đo chu kỳ thực tế                                      │
│     ↓                                                        │
│  5. Nhấn "End" hoặc "Reset"                                  │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔍 Các trạng thái của hệ thống

### **Ideal Mode - PreExperiment (Setup Phase)**
- ✅ `isKinematic = true` (con lắc đứng yên)
- ✅ `enforceAngleLimit = true` (giới hạn ±15°)
- ✅ Cho phép ConstrainedGrab
- ✅ Hiển thị góc hiện tại trong Console
- ❌ IdealPendulumSimulator chưa chạy

### **Ideal Mode - Running**
- ✅ `isKinematic = true` (script điều khiển)
- ✅ `enforceAngleLimit = false` (không giới hạn)
- ✅ IdealPendulumSimulator đang chạy
- ✅ Dao động từ góc đã setup

### **Realistic Mode - PreExperiment**
- ✅ `isKinematic = false` (vật lý thông thường)
- ❌ Không có Setup Phase
- ❌ Không giới hạn góc

### **Realistic Mode - Running**
- ✅ `isKinematic = false` (physics engine)
- ✅ Áp dụng damping
- ✅ Đo chu kỳ thực tế

---

## 🐛 Xử lý lỗi

### **Vấn đề: Không thể kéo con lắc để setup**
**Nguyên nhân 1:** Mode = Realistic  
**Giải pháp:** Chuyển sang Ideal Mode trong Inspector

**Nguyên nhân 2:** Đã bấm Start  
**Giải pháp:** Bấm Reset trước

### **Vấn đề: Con lắc rơi xuống khi thả ra (trong Ideal Mode)**
**Nguyên nhân:** Lỗi logic, `isKinematic` không được set đúng  
**Kiểm tra:** Trong Setup Phase (Ideal), `isKinematic` phải là `true`

### **Vấn đề: Bấm Start nhưng con lắc không dao động (Ideal Mode)**
**Nguyên nhân:** IdealPendulumSimulator không được kích hoạt  
**Kiểm tra:** Console phải có log "IdealPendulumSimulator đã được kích hoạt"

---

## 📊 Chi tiết kỹ thuật

### **CheckAssemblyState() Logic**
```csharp
// Setup phase CHỈ cho Ideal Mode + PreExperiment
bool isIdealSetupPhase = (mode == SimulationMode.Ideal && 
                          CurrentState == ExperimentState.PreExperiment);

if (isIdealSetupPhase)
{
    // Giới hạn góc + Đặt kinematic
    pendulumBob.ConfigureSnappedPhysics(true); // isKinematic = true
}
else if (mode == SimulationMode.Realistic)
{
    // Không giới hạn + vật lý thông thường
    pendulumBob.ConfigureSnappedPhysics(false); // isKinematic = false
}
```

### **StartExperimentLogic() Flow**
```csharp
if (_isInSetupPhase && mode == SimulationMode.Ideal)
{
    // Thoát setup phase
    // IdealPendulumSimulator sẽ đọc góc hiện tại trong StartSimulation()
    ApplySimulationMode(); // Kích hoạt simulator từ góc setup
}
```

---

## ✅ Checklist test

### **Ideal Mode:**
- [ ] Chọn Mode = Ideal trong Inspector
- [ ] Lắp ráp con lắc thành công
- [ ] Console hiện "Đang ở giai đoạn Setup" (Ideal Mode)
- [ ] Kéo con lắc → góc bị giới hạn ±15°
- [ ] Thả ra → con lắc đứng yên (không rơi)
- [ ] Console log "Góc hiện tại: X°. Con lắc đang đứng yên."
- [ ] Bấm Start → IdealPendulumSimulator kích hoạt
- [ ] Con lắc dao động từ góc đã setup
- [ ] Bấm Reset → quay về trạng thái ban đầu

### **Realistic Mode:**
- [ ] Chọn Mode = Realistic trong Inspector
- [ ] Lắp ráp con lắc thành công
- [ ] Console KHÔNG hiện "Setup Phase"
- [ ] KHÔNG thể kéo con lắc để setup
- [ ] Bấm Start → con lắc dao động theo physics
- [ ] Damping được áp dụng đúng

---

## 🎓 Ý nghĩa vật lý

### **Ideal Mode:**
Mô phỏng dao động điều hòa lý tưởng:
- Không có ma sát, lực cản
- `sin(θ) ≈ θ` (góc nhỏ < 15°)
- Chu kỳ: `T = 2π√(L/g)`
- Năng lượng bảo toàn

### **Realistic Mode:**
Dao động với lực cản:
- Có damping (ma sát không khí)
- Biên độ giảm dần theo thời gian
- Chu kỳ gần bằng lý thuyết nhưng không hoàn hảo

---

**Tài liệu được cập nhật: 2025-11-14**
**Phiên bản: 2.0 - Ideal Mode Only Setup**
