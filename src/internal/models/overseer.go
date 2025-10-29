package models

// OverseerPageInfo represents pagination information from Overseer API
type OverseerPageInfo struct {
	Pages       int  `json:"pages"`
	PageSize    int  `json:"pageSize"`
	Results     int  `json:"results"`
	Page        int  `json:"page"`
	HasNextPage bool `json:"hasNextPage"`
}