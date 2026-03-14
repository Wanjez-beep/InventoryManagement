const API_URL = "http://localhost:5267/api/products";

async function loadProducts() {
    const response = await fetch(API_URL);
    const products = await response.json();

    const table = document.getElementById("productTable");
    table.innerHTML = "";

    products.forEach(p => {
        table.innerHTML += `
            <tr ${p.quantity < 5 ? 'style="background-color:#ffdddd;"' : ''}>
                <td>${p.name}</td>
                <td>${p.category}</td>
                <td>${p.quantity}</td>
                <td>$${p.price}</td>
                <td>
                    <button onclick="deleteProduct(${p.id})">Delete</button>
                </td>
            </tr>
        `;
    });
}

async function addProduct() {
    const product = {
        name: document.getElementById("name").value,
        category: document.getElementById("category").value,
        quantity: parseInt(document.getElementById("quantity").value),
        price: parseFloat(document.getElementById("price").value)
    };

    await fetch(API_URL, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(product)
    });

    loadProducts();
}

async function deleteProduct(id) {
    await fetch(`${API_URL}/${id}`, { method: "DELETE" });
    loadProducts();
}

function searchProducts() {
    const filter = document.getElementById("search").value.toLowerCase();
    const rows = document.querySelectorAll("tbody tr");

    rows.forEach(row => {
        row.style.display = row.innerText.toLowerCase().includes(filter)
            ? ""
            : "none";
    });
}

loadProducts();