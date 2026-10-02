# Production → Inventory Acceptance

1. Create a production batch.
2. Add ingredient lines.
3. Start the batch.
4. Record actual ingredient quantities.
5. Complete with actual yield and waste.
6. Verify actual quantities are persisted.
7. Verify one `CONSUMPTION` inventory movement per ingredient.
8. Verify inventory decreases exactly by actual consumption.
9. Verify production waste is persisted.
10. Verify planned cost, actual cost and cost variance.
11. Verify the batch becomes `COMPLETED`.
12. Attempt completion with insufficient stock.
13. Verify the entire transaction rolls back:
    - no consumption movements,
    - no waste record,
    - batch remains `INPROGRESS`.
14. Attempt completion against a stock location belonging to another branch.
15. Verify rejection and no state change.
