```markdown
# Quickstart: Product import search & selection (feature 007)

1. Ensure server is running locally (http://localhost:5000)

2. Open the product import UI in the client app

3. Enter a product code or name in the search box and press Search

4. Select one or more results (checkboxes) — Import is disabled until selection present

5. Click Import — verify the API call to `/api/products/import` contains only selected product ids

6. Verify import result summary shows counts for created vs updated items

``` 
