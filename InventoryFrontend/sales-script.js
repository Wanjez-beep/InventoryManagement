const API_URL_PRODUCTS = "http://localhost:5267/api/products";
const API_URL_SALES = "http://localhost:5267/api/sales";

let allSales = [];

// Initialize page
async function init() {
    await loadProducts();
    await loadSales();
    await loadProfitSummary();
}

// Load products dropdown
async function loadProducts() {
    try {
        const response = await fetch(API_URL_PRODUCTS);
        const products = await response.json();

        const productSelect = document.getElementById("productId");
        productSelect.innerHTML = '<option value="">-- Select Product --</option>';

        products.forEach(p => {
            const option = document.createElement("option");
            option.value = p.id;
            option.textContent = `${p.name} (Stock: ${p.quantity})`;
            option.dataset.costPrice = p.costPrice || 0;
            productSelect.appendChild(option);
        });
    } catch (error) {
        showAlert("Error loading products: " + error.message, "error");
    }
}

// Auto-fill product details when selected
async function loadProductDetails() {
    const productId = document.getElementById("productId").value;
    const productSelect = document.getElementById("productId");
    
    if (!productId) {
        document.getElementById("productName").value = "";
        document.getElementById("costPrice").value = "";
        return;
    }

    try {
        const response = await fetch(`${API_URL_PRODUCTS}/${productId}`);
        const product = await response.json();
        
        document.getElementById("productName").value = product.name;
        document.getElementById("costPrice").value = product.costPrice || 0;
    } catch (error) {
        showAlert("Error loading product details: " + error.message, "error");
    }
}

// Product selection change handler
document.addEventListener("DOMContentLoaded", function() {
    const productSelect = document.getElementById("productId");
    productSelect.addEventListener("change", loadProductDetails);
    init();
});

// Record a sale
async function recordSale() {
    const productId = document.getElementById("productId").value;
    const quantitySold = parseInt(document.getElementById("quantitySold").value);
    const costPrice = parseFloat(document.getElementById("costPrice").value);
    const sellingPrice = parseFloat(document.getElementById("sellingPrice").value);
    const notes = document.getElementById("notes").value;

    if (!productId || !quantitySold || !costPrice || !sellingPrice) {
        showAlert("Please fill in all required fields", "error");
        return;
    }

    if (quantitySold <= 0) {
        showAlert("Quantity must be greater than 0", "error");
        return;
    }

    if (costPrice < 0 || sellingPrice < 0) {
        showAlert("Prices cannot be negative", "error");
        return;
    }

    try {
        const saleData = {
            productId: parseInt(productId),
            quantitySold: quantitySold,
            costPrice: costPrice,
            sellingPrice: sellingPrice,
            notes: notes
        };

        const response = await fetch(API_URL_SALES, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(saleData)
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || "Error recording sale");
        }

        showAlert("Sale recorded successfully!", "success");
        
        // Clear form
        document.getElementById("productId").value = "";
        document.getElementById("productName").value = "";
        document.getElementById("quantitySold").value = "";
        document.getElementById("costPrice").value = "";
        document.getElementById("sellingPrice").value = "";
        document.getElementById("notes").value = "";

        // reload data
        await loadProducts();
        await loadSales();
        await loadProfitSummary();
    } catch (error) {
        showAlert("Error: " + error.message, "error");
    }
}

// Load all sales
async function loadSales() {
    try {
        const response = await fetch(API_URL_SALES);
        allSales = await response.json();

        const table = document.getElementById("salesTable");
        table.innerHTML = "";

        if (allSales.length === 0) {
            table.innerHTML = "<tr><td colspan='9' style='text-align: center; padding: 20px;'>No sales recorded yet</td></tr>";
            return;
        }

        allSales.forEach(sale => {
            const saleDate = new Date(sale.saleDate).toLocaleString();
            const profitPerUnit = (sale.sellingPrice - sale.costPrice).toFixed(2);
            const totalProfit = sale.totalProfit.toFixed(2);

            table.innerHTML += `
                <tr>
                    <td>${sale.productName}</td>
                    <td>${sale.quantitySold}</td>
                    <td>Ksh ${parseFloat(sale.costPrice).toFixed(2)}</td>
                    <td>Ksh ${parseFloat(sale.sellingPrice).toFixed(2)}</td>
                    <td class="profit-positive">Ksh ${profitPerUnit}</td>
                    <td class="profit-positive">Ksh ${totalProfit}</td>
                    <td>${saleDate}</td>
                    <td>${sale.notes || "-"}</td>
                    <td>
                        <button class="action-btn delete-btn" onclick="reverseSale(${sale.id})">Reverse</button>
                    </td>
                </tr>
            `;
        });
    } catch (error) {
        showAlert("Error loading sales: " + error.message, "error");
    }
}

// Load profit summary
async function loadProfitSummary() {
    try {
        const response = await fetch(`${API_URL_SALES}/profit/summary`);
        const summary = await response.json();

        document.getElementById("totalSales").textContent = summary.totalSales;
        document.getElementById("totalQuantity").textContent = summary.totalQuantitySold;
        document.getElementById("totalRevenue").textContent = `Ksh ${parseFloat(summary.totalRevenue).toFixed(2)}`;
        
        const profitClass = summary.totalProfit >= 0 ? "profit-positive" : "profit-negative";
        document.getElementById("totalProfit").className = `value ${profitClass}`;
        document.getElementById("totalProfit").textContent = `Ksh ${parseFloat(summary.totalProfit).toFixed(2)}`;
        
        const margin = parseFloat(summary.profitMargin).toFixed(2);
        document.getElementById("profitMargin").textContent = `Margin: ${margin}%`;
    } catch (error) {
        console.error("Error loading profit summary:", error.message);
    }
}

// Reverse a sale
async function reverseSale(saleId) {
    if (!confirm("Are you sure you want to reverse this sale? Inventory will be restored.")) {
        return;
    }

    try {
        const response = await fetch(`${API_URL_SALES}/${saleId}`, {
            method: "DELETE"
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || "Error reversing sale");
        }

        showAlert("Sale reversed successfully!", "success");
        await loadSales();
        await loadProfitSummary();
        await loadProducts();
    } catch (error) {
        showAlert("Error: " + error.message, "error");
    }
}

// Search sales
function searchSales() {
    const filter = document.getElementById("search").value.toLowerCase();
    const rows = document.querySelectorAll("tbody tr");

    rows.forEach(row => {
        row.style.display = row.innerText.toLowerCase().includes(filter) ? "" : "none";
    });
}

// Show alert message
function showAlert(message, type) {
    const alert = document.getElementById("alert");
    alert.textContent = message;
    alert.className = `alert ${type}`;
    
    if (type === "success") {
        setTimeout(() => {
            alert.className = "alert";
        }, 3000);
    }
}
