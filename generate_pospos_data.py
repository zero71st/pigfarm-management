import json
import random
from datetime import datetime, timedelta

# Jet feed products with realistic pricing
jet_products = [
    {"code": "PK64000158", "name": "เจ็ท 105 หมูเล็ก 6-15 กก.", "price": 755, "type": "starter"},
    {"code": "PK64000159", "name": "เจ็ท 108 หมูนม 15-25 กก.", "price": 650, "type": "nursery"},
    {"code": "PK64000160", "name": "เจ็ท 110 หมู 25-40 กก.", "price": 595, "type": "grower"},
    {"code": "PK64000161", "name": "เจ็ท 120 หมู 40-60 กก.", "price": 580, "type": "finisher1"},
    {"code": "PK64000162", "name": "เจ็ท 130 หมู 60-90 กก.", "price": 565, "type": "finisher2"},
    {"code": "PK64000163", "name": "เจ็ท 153 หมู 90 กก. ขึ้นไป", "price": 550, "type": "finisher3"}
]

# Customer names (matching the database)
customer_names = [
    "Robert Ranch 1", "Siriporn Agricultural 2", "Siriporn Agriculture 3", "Siriporn Pig Farm 4", "Sarah Ranch 5",
    "Niran Farm 6", "Robert Pig Farm 7", "Niran Livestock Ltd 8", "Jennifer Livestock 9", "Jennifer Swine Ranch 10",
    "David Swine Ranch 11", "Lisa Farm 12", "Siriporn Farm 13", "Robert Farm 14", "Michael Pig Farm 15",
    "John Farm 16", "Malee Farm Corp 17", "John Livestock 18", "Lisa Farming Co 19", "Jennifer Farm 20",
    "Robert Ranch 21", "Jennifer Ranch 22", "Lisa Farm Corp 23", "David Pig Farm 24", "Sarah Farm Corp 25",
    "John Agriculture 26", "James Ranch 27", "Jennifer Livestock Ltd 28", "Malee Agriculture 29", "Niran Swine Ranch 30",
    "Somchai Farm 31", "Jennifer Ranch 32", "Niran Pig Farm 33", "Sarah Livestock Ltd 34", "Michael Pig Farm 35",
    "Mary Farm 36", "Michael Pig Farm 37", "James Livestock Ltd 38", "Ploy Farm Corp 39", "Sarah Pig Farm 40",
    "Ploy Ranch 41", "Ploy Farm 42", "Jennifer Farm Corp 43", "James Ranch 44", "Lisa Agricultural 45",
    "Lisa Agricultural 46", "Niran Ranch 47", "Siriporn Livestock 48", "Malee Farming Co 49", "Ploy Farm Corp 50",
    "Somchai Farm Corp 51", "James Farm 52", "Malee Livestock Ltd 53", "Niran Farm 54", "Ploy Farm Corp 55",
    "John Livestock 56", "Lisa Livestock 57", "Ploy Swine Ranch 58", "Somchai Livestock Ltd 59", "John Pig Farm 60",
    "Ploy Ranch 61", "Somchai Swine Ranch 62", "Ploy Farming Co 63", "Niran Pig Farm 64", "James Swine Ranch 65",
    "Suchart Swine Ranch 66", "Michael Farm Corp 67", "Michael Farming Co 68", "John Farm Corp 69", "Robert Farming Co 70",
    "Michael Farm Corp 71", "James Livestock Ltd 72", "Lisa Livestock 73", "Ploy Farm Corp 74", "Somchai Livestock 75",
    "John Agriculture 76", "Sarah Pig Farm 77", "Niran Farm 78", "Lisa Agricultural 79", "Robert Livestock Ltd 80",
    "Jennifer Livestock Ltd 81", "Mary Agricultural 82", "Robert Farm Corp 83", "Malee Ranch 84", "Niran Farm Corp 85",
    "Sarah Livestock Ltd 86", "Mary Livestock Ltd 87", "Niran Swine Ranch 88", "Jennifer Ranch 89", "Niran Farm Corp 90",
    "Lisa Swine Ranch 91", "Malee Farm Corp 92", "Siriporn Farming Co 93", "Suchart Farming Co 94", "Siriporn Livestock Ltd 95",
    "Michael Agricultural 96", "Sarah Ranch 97", "Lisa Livestock 98", "Ploy Agricultural 99", "Sarah Agriculture 100"
]

# Pig pen sizes (matching realistic pig counts per customer)
pig_pen_sizes = [
    17, 10, 40, 22, 11, 19, 38, 41, 14, 15, 18, 19, 13, 24, 26, 35, 41, 46, 24, 45,
    32, 35, 16, 17, 17, 22, 22, 16, 47, 38, 36, 29, 42, 35, 14, 22, 39, 18, 29, 48,
    24, 41, 27, 14, 41, 48, 45, 10, 14, 42, 17, 27, 19, 25, 42, 12, 41, 19, 17, 28,
    31, 34, 40, 48, 44, 36, 20, 32, 43, 32, 22, 48, 47, 47, 44, 48, 45, 48, 20, 14,
    22, 14, 28, 15, 18, 37, 41, 33, 15, 24, 46, 34, 45, 47, 26, 31, 36, 26, 47, 11
]

# Shop names for variety
shop_names = [
    "ร้านอาหารสัตว์เจ็ท สาขา 1", "ร้านอาหารสัตว์เจ็ท สาขา 2", "ร้านอาหารสัตว์เจ็ท สาขา 3", 
    "ร้านอาหารสัตว์เจ็ท สาขา 4", "ร้านอาหารสัตว์เจ็ท สาขา 5", "ร้านอาหารสัตว์เจ็ท สาขา 6",
    "ร้านอาหารสัตว์เจ็ท สาขา 7", "ร้านอาหารสัตว์เจ็ท สาขา 8", "ร้านอาหารสัตว์เจ็ท สาขา 9",
    "ร้านอาหารสัตว์เจ็ท สาขา 10"
]

# Generate transactions
transactions = []

# Set random seed for reproducible data
random.seed(42)

for i in range(1, 101):  # 100 customers
    customer_code = f"M{i:06d}"
    customer_name = customer_names[i-1]
    pig_count = pig_pen_sizes[i-1]
    
    # Generate 1-3 transactions per customer
    num_transactions = random.randint(1, 3)
    
    for tx_num in range(num_transactions):
        transaction_id = len(transactions) + 1
        
        # Calculate appropriate feed quantity based on pig count
        # Each pig typically needs 2-3 bags per month, with some variation
        base_bags_per_pig = random.uniform(1.8, 3.2)
        total_bags_needed = int(pig_count * base_bags_per_pig)
        
        # Create 1-2 feed products per transaction
        num_products = random.randint(1, 2)
        order_list = []
        
        remaining_bags = total_bags_needed
        for prod_num in range(num_products):
            if remaining_bags <= 0:
                break
                
            # Select appropriate feed product based on pig stage
            if pig_count <= 20:  # Small farms get starter/nursery feeds
                product = random.choice(jet_products[:3])
            elif pig_count <= 35:  # Medium farms get grower/finisher feeds
                product = random.choice(jet_products[2:5])
            else:  # Large farms get finisher feeds
                product = random.choice(jet_products[3:])
            
            # Determine quantity for this product
            if num_products == 1:
                stock = remaining_bags
            else:
                if prod_num == num_products - 1:  # Last product
                    stock = remaining_bags
                else:
                    stock = random.randint(remaining_bags // 3, (remaining_bags * 2) // 3)
            
            remaining_bags -= stock
            
            # Add price variation (±5%)
            price_variation = random.uniform(0.95, 1.05)
            actual_price = int(product["price"] * price_variation)
            
            # Calculate discount (0-3% typically)
            discount_per_bag = random.randint(0, 15)
            
            order_item = {
                "stock": stock,
                "name": product["name"],
                "price": actual_price,
                "special_price": actual_price,
                "discount_type": 1,
                "cost_discount_price": discount_per_bag,
                "code": product["code"],
                "sku_list": [],
                "topping_in_order": [],
                "total_price_include_discount": stock * actual_price,
                "note_in_order": [
                    f"คลังฟาร์ม {chr(65 + (transaction_id % 26))}"  # A-Z rotation
                ]
            }
            order_list.append(order_item)
        
        # Calculate totals
        sub_total = sum(item["total_price_include_discount"] for item in order_list)
        
        # Generate realistic timestamp (last 30 days)
        days_ago = random.randint(1, 30)
        timestamp = datetime.now() - timedelta(days=days_ago)
        
        # Generate phone number
        phone_prefix = random.choice(["081", "082", "083", "084", "085", "086", "087", "088", "089", "090", "091", "092", "093", "094", "095", "096", "097", "098", "099", "020"])
        phone_suffix = f"{random.randint(100, 999)}-{random.randint(1000, 9999)}"
        phone = f"{phone_prefix}-{phone_suffix}"
        
        transaction = {
            "_id": f"{random.randint(100000000000000000000000, 999999999999999999999999):024x}",
            "discount": 0,
            "tax_percent": 0,
            "code": f"TR68{transaction_id:06d}",
            "order_list": order_list,
            "timestamp": timestamp.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z",
            "comment": "",
            "return_date": None,
            "register_vat": False,
            "segment_type": 4,
            "sub_total": sub_total,
            "sub_total_exclude_vat": 0,
            "grand_total": sub_total,
            "cash_register": None,
            "buyer_detail": {
                "code": customer_code,
                "firstname": customer_name,
                "lastname": "",
                "phone": phone
            },
            "shop_detail": {
                "shop_id": f"shop{((transaction_id-1) % 10) + 1:03d}",
                "shop_name": shop_names[(transaction_id-1) % 10]
            }
        }
        
        transactions.append(transaction)

# Sort transactions by code
transactions.sort(key=lambda x: x["code"])

# Write to file
with open("feeds_comprehensive.json", "w", encoding="utf-8") as f:
    json.dump(transactions, f, ensure_ascii=False, indent=2)

print(f"Generated {len(transactions)} transactions for 100 customers")
print("File saved as feeds_comprehensive.json")