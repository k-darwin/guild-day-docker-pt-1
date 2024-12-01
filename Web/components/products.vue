<script setup>
const runtimeConfig = useRuntimeConfig();
const products = ref([]);
const basket = ref({});

const updateBasket = (product) => {
  if (basket.value.id == null || basket.value.id == undefined) {
    fetch(`${runtimeConfig.public.apiGateWayEndpoint}basket/basket`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify([
        {
          name: product.name,
          price: product.cost,
          quantity: 1,
          productId: product.id,
        },
      ]),
    })
      .then((response) => response.json())
      .then((data) => {
        getBasket(data.id);
      });
  } else {
    fetch(
      `${runtimeConfig.public.apiGateWayEndpoint}basket/basket/${basket.value.id}`,
      {
        method: "put",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify([
          {
            name: product.name,
            price: product.cost,
            quantity: 1,
            productId: product.id,
          },
        ]),
      }
    )
      .then((response) => response.json())
      .then((data) => {
        getBasket(basket.value.id);
      });
  }
};

const getBasket = (id) => {
  fetch(`${runtimeConfig.public.apiGateWayEndpoint}basket/basket/${id}`)
    .then((response) => response.json())
    .then((data) => {
      basket.value = data;
    });
};

const placeOrder = () => {
  fetch(`${runtimeConfig.public.apiGateWayEndpoint}order/orders`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      cost: basket.value.items.reduce((sum, x) => sum + x.price, 0),
      orderItems: basket.value.items.map((item, index) => ({
        productId: item.productId,
        quantity: item.quantity,
        cost: item.price,
      })),
    }),
  }).then(() => {
    basket.value = {};
  });
};

onMounted(() => {
  fetch(`${runtimeConfig.public.apiGateWayEndpoint}product/products`)
    .then((response) => response.json())
    .then((data) => {
      products.value = data;
    });
});
</script>

<template>
  <div>
    <h1 class="text-center my-4">Products</h1>
    <ul class="list-group mb-4">
      <li
        class="list-group-item d-flex justify-content-between align-items-center"
        v-for="product in products"
        :key="product.id"
      >
        {{ product }}
        <button
          class="btn btn-primary btn-sm"
          @click="updateBasket(product)"
        >
          Add to basket
        </button>
      </li>
    </ul>

    <h1 class="text-center my-4">Basket</h1>
    <ul class="list-group mb-4">
      <li
        class="list-group-item"
        v-for="product in basket"
        :key="product.id"
      >
        {{ product }}
      </li>
    </ul>
    <div class="text-center">
      <button class="btn btn-success" @click="placeOrder()">
        Place Order
      </button>
    </div>
  </div>
</template>


<style scoped></style>
