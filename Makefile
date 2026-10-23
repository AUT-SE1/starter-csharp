.PHONY: help run test coverage contract-test sandbox sandbox-stop

HOST_PORT ?= 8080
TEST_PROJECT = tests/StarterService.Tests

help:
	@echo "make run            Start the service on 0.0.0.0:$(HOST_PORT) so your neighbour can reach it"
	@echo "make test           Run the unit and integration tests"
	@echo "make coverage       Run the tests and collect coverage"
	@echo "make contract-test  Run the contract test against NEIGHBOR_BASE_URL"
	@echo "make sandbox        Same as run, but in Docker"

## Bind to 0.0.0.0, not 127.0.0.1 — on localhost your neighbour cannot reach you.
run:
	dotnet run --urls http://0.0.0.0:$(HOST_PORT)

test:
	dotnet test $(TEST_PROJECT)

coverage:
	dotnet test $(TEST_PROJECT) --collect:"XPlat Code Coverage"
	@echo "The Cobertura report is under $(TEST_PROJECT)/TestResults/."

## Session 05. Point this at the neighbouring team's running sandbox:
##   make contract-test NEIGHBOR_BASE_URL=http://10.0.0.42:8080
contract-test:
	@test -n "$(NEIGHBOR_BASE_URL)" || \
		{ echo "Set NEIGHBOR_BASE_URL, e.g. make contract-test NEIGHBOR_BASE_URL=http://10.0.0.42:8080"; exit 1; }
	NEIGHBOR_BASE_URL=$(NEIGHBOR_BASE_URL) dotnet test $(TEST_PROJECT) \
		--filter FullyQualifiedName~NeighborContractTests --logger "console;verbosity=detailed"

sandbox:
	docker compose up --build -d
	@echo "Listening on 0.0.0.0:$(HOST_PORT). Tell your neighbour: http://<your-ip>:$(HOST_PORT)"

sandbox-stop:
	docker compose down
