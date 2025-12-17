-- Initialize database
CREATE DATABASE IF NOT EXISTS filestorage;

-- Connect to the database
\c filestorage;

-- Run schema
\i schema.sql

