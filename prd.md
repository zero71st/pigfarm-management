# PigTrack Pro - Product Requirements Document (PRD)

## 1. ชื่อโปรเจค

**PigFarmManagement** – ระบบจัดการคอกหมูและติดตามการขายอาหาร

## 2. วัตถุประสงค์

* จัดการทะเบียนคอกหมูของลูกค้า (Cash / Project)
* บันทึกและติดตามการซื้อ/ขายอาหารของแต่ละคอก
* บันทึกเงินมัดจำหลายรอบสำหรับ Project Customer
* คำนวณค่าอาหาร, เงินลงทุน, กำไร/ขาดทุน, ยอดเงินคงเหลือของลูกค้า
* คำนวณวันเริ่มขุน, วันสิ้นสุดขุน, วันจับหมู และสัปดาห์จับหมู
* ดึงข้อมูลลูกค้าและบิลขายอาหารจาก POSPOS API
* พิมพ์ใบสรุปปิดคอกและใบจ่ายอาหาร
* รายงานสรุปยอดเงินร้านและมัดจำลูกค้าทั้งหมด

## 3. กลุ่มผู้ใช้งาน (User Types)

| ประเภทผู้ใช้ | สิทธิ์การเข้าถึง                                                                       |
| ------------ | -------------------------------------------------------------------------------------- |
| Admin        | Sync ลูกค้า/บิล, จัดการคอกหมู, บันทึกอาหาร, มัดจำ, ผลจับหมู, ดูรายงาน, ตั้งค่า deposit |
| Staff        | บันทึกอาหาร, มัดจำ, ผลจับหมู, ดูรายงาน                                                 |
| Viewer       | ดูคอกหมู, ดูรายงาน, ดูสรุปยอดเงิน                                                      |

## 4. Feature Architecture

| Feature                | Description                             | Sub-Features                                                                                                                                      |
| ---------------------- | --------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| Customer Management    | ดึงข้อมูลลูกค้า                         | Fetch Customer from POSPOS API, Assign CustomerType (Cash/Project)                                                                                |
| Pig Pen Management     | จัดการคอกหมู                            | Add/Edit/Delete Pig Pen, Auto-calc Start/End/Est. Harvest, Deposit auto-calc                                                                      |
| Deposit Management     | บันทึกมัดจำ                             | Multiple deposits per Pig Pen, Auto-calc for Project Customers                                                                                    |
| Feed Management        | บันทึกอาหาร                             | Sync bill from POSPOS API (Type, Qty, Price, Cost), Manual add/edit, Calc total feed cost                                                         |
| Harvest Management     | บันทึกผลจับหมู                          | Record date, avg/max/min weight, number of pigs, Update Financial Calculation                                                                     |
| Financial Calculation  | คำนวณเงินลงทุน, กำไร/ขาดทุน, ยอดคงเหลือ | Investment = FeedCost - Deposit, Profit/Loss = (Weight × SalePrice) - FeedCost, Net balance = (WeightSold × SalePrice) - (Feed + Other) + Deposit |
| Reporting & Printing   | รายงานและเอกสารสรุป                     | Summary per Pig Pen/Customer, Print harvest/feeding summary, Export Excel/PDF                                                                     |
| User & Role Management | ระบบผู้ใช้งาน                           | Add/Edit/Delete users, Assign roles, JWT Authentication & Authorization                                                                           |

## 5. Workflow – หน้าเดียวหลัก

```
[Search / Filter Customer] --> [Pig Pen List Table (All Customers)]
                                    |
                                    v
                      [Add Pig Pen Form] --(Save)--> [PigPen Table]
                                    |
                                    v
                      [Feed History Table (from POSPOS API)]
                                    |
                                    v
                      [Add Feed Form] --(Save)--> [Feed Table]
                                    |
                                    v
                           [Summary Panel]
                                    |
                                    v
                           [Print / Export Buttons]
```

## 6. Wireframe หน้า Pig Pen List + หน้าเดียว

```
------------------------------------------------------
| Filter Customer: [All Customers ▼] [Search]      |
------------------------------------------------------
| Pig Pen List Table:                               |
| ------------------------------------------------ |
| | Customer | Pen | Qty | Start | End | Est. Harvest | Feed Cost | Investment | Profit/Loss | Actions |
| ------------------------------------------------ |
| | John     | P001 | 20 | 01/06 | 29/09 | 29/10       | 15,000     | 12,000     | 3,000       | [View] [Edit] |
| | Mary     | P002 | 15 | 05/06 | 03/10 | 03/11       | 11,000     | 9,000      | 2,000       | [View] [Edit] |
| | John     | P003 | 10 | 10/06 | 08/10 | 08/11       | 8,000      | 7,000      | 1,000       | [View] [Edit] |
| ------------------------------------------------ |
| [Add New Pig Pen]                                 |
------------------------------------------------------
| Feed History Table:                               |
| Type | Qty | Price | Cost | Date                 |
| ------------------------------------------------ |
| Add Feed Form:                                   |
| Type: [_____] Qty: [__] Price: [_____] [Add]    |
------------------------------------------------------
| Summary Panel:                                   |
| Total Feed Cost: XXXX                            |
| Investment: XXXX                                 |
| Profit/Loss: XXXX                                |
------------------------------------------------------
| [Print Feed Sheet] [Print Closing Summary]      |
------------------------------------------------------
```

## 7. Tech Stack

| ส่วน         | เทคโนโลยี                                                     |
| ------------ | ------------------------------------------------------------- |
| Front-end    | Blazor WebAssembly (SPA, Mobile-friendly)                     |
| Back-end     | .NET Core Web API (RESTful API สำหรับ CRUD, Sync POSPOS API)  |
| Database     | Supabase (PostgreSQL)                                         |
| External API | POSPOS API สำหรับดึงข้อมูลลูกค้าและบิลขายอาหาร                |
| Security     | HTTPS, JWT Authentication, Role-based Access Control          |
| Hosting      | Front-end: Vercel/Netlify, Back-end + Supabase: Railway/Azure |
