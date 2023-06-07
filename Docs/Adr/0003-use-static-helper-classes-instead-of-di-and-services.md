# 1. Use static helper classes instead of DI and services

2023-06-07

## Status

Accepted

## Context

This console app is simple enough that it does not require any fancy dependency injections. Thus, each "service" class appears as a static helper class instead

## Decision

It is spoken

## Consequences

May need DI later as the program evolves