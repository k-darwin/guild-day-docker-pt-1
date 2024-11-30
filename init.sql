-- Create the products table
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    cost DECIMAL(10, 2) NOT NULL
);
-- Insert products into the products table
INSERT INTO products (name, description, cost) VALUES
('Toy', 'Description for toy', 10.00),
('Game', 'Description for game', 20.00),
('Book', 'Description for book', 30.00),
('Gadget', 'Description for gadget', 40.00),
('Accessory', 'Description for accessory', 50.00);

-- Create the orders table
CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    cost DECIMAL(10, 2) NOT NULL
);

-- Create the order_items table
CREATE TABLE order_items (
    id SERIAL PRIMARY KEY,
    order_id INT NOT NULL,
    product_id INT NOT NULL,
    quantity INT NOT NULL,
    cost DECIMAL(10, 2) NOT NULL
);